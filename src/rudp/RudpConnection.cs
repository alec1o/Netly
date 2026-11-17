using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Netly
{
    // selective repeat connection: window, cumulative ack + sack, rtt based retransmission, fragmentation.
    // all the state is protected by _locker, callbacks and socket reads never run inside it.
    internal sealed class RudpConnection
    {
        private enum State
        {
            Idle,
            Handshaking,
            Connected,
            Closed
        }

        private class Outgoing
        {
            public byte[] Data;
            public uint Seq;
            public int PayloadSize;
            public long SentAt;
            public int Retransmits;
            public bool Sacked, FastRetransmitted;
        }

        private struct Fragment
        {
            public byte Flags;
            public byte[] Data;
        }

        private class Assembly
        {
            public byte[][] Parts;
            public int Received, Length;
            public long Deadline;
            public uint Seq;
        }

        // events of a locked section, they are raised after the lock is released
        private struct Pending
        {
            public bool Opened, Closed;
            public string Fail, Error;
            public List<KeyValuePair<byte[], RudpDelivery>> Messages;

            public void Add(byte[] data, RudpDelivery type)
            {
                if (Messages == null) Messages = new List<KeyValuePair<byte[], RudpDelivery>>();
                Messages.Add(new KeyValuePair<byte[], RudpDelivery>(data, type));
            }
        }

        private const long AckDelay = 2;
        private const long MinRto = 100, MaxRto = 1500, InitialRto = 200;
        private const long DatagramLifetime = 2000;
        private const int MaxRetransmits = 20;
        private const int DuplicateThreshold = 3;
        private const int MaxReorder = 2048;
        private const int MaxAssemblies = 16;
        private const int MaxWindow = 1024;
        private const int InitialWindow = 32;
        private const long MaxPendingBytes = 64L * 1024 * 1024;
        private const int MaxInFlightBytes = 1024 * 1024;

        private readonly object _locker = new object();
        private readonly Socket _socket;
        private readonly IPEndPoint _remote;
        private readonly bool _isServer;
        private readonly byte[] _control = new byte[RudpWire.ControlHeader];

        private volatile State _state = State.Idle;
        private uint _token, _nonce;
        private long _startedAt, _nextHandshakeAt, _handshakeDelay, _synSentAt;
        private long _nextTickAt;
        private readonly int _localDatagram;
        private bool _synRetransmitted;
        private long _lastReceiveAt, _lastSendAt;

        // send
        private readonly List<Outgoing> _out = new List<Outgoing>();
        private int _head, _sent, _sackedCount;
        private uint _nextSeq, _sentEnd;
        private long _pendingBytes;
        private double _cwnd = InitialWindow, _ssthresh = MaxWindow;
        private bool _inRecovery;
        private uint _recoveryPoint;
        private double _srtt, _rttvar;
        private long _rto = InitialRto;
        private bool _hasRtt;

        // receive
        private readonly Dictionary<uint, Fragment> _reorder = new Dictionary<uint, Fragment>();
        // reliable message being assembled
        private byte[] _assemblyData;
        private int _assemblyPosition;
        private uint _nextExpected;
        private int _unackedReceived;
        private long _ackDueAt;

        // datagrams
        private readonly Dictionary<ushort, Assembly> _assemblies = new Dictionary<ushort, Assembly>();
        private uint _sendSequenced, _lastSequenced;
        private bool _hasSequenced;
        private ushort _nextMessageId;

        public RudpConnection(Socket socket, IPEndPoint remote, bool isServer)
        {
            _socket = socket;
            _remote = remote;
            _isServer = isServer;
            // the usual size of the path, never above what the send and receive buffers of the socket hold
                _localDatagram = SocketLimits.Both(socket,
                    IPAddress.IsLoopback(remote.Address) ? RudpWire.LoopbackDatagram : RudpWire.InternetDatagram);

                // until the handshake ends it is the limit of this side, then the smaller one of the two peers
                MaxDatagram = _localDatagram;
        }

        public string Id { get; private set; } = string.Empty;
        public int MaxDatagram { get; private set; }

        // biggest message accepted, a bigger one closes the connection
        public long MaxMessageSize { get; set; } = Framing.DefaultSize;
        public int HandshakeTimeout { get; set; } = 5000;
        public int NoResponseTimeout { get; set; } = 5000;
        public bool IsOpened => _state == State.Connected;
        public bool IsClosed => _state == State.Closed;

        // the scheduler skips the connection until this time, 0 means right now
        public long NextTickAt => Interlocked.Read(ref _nextTickAt);

        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        public Action<string> OnOpenFail { get; set; }

        // raised before OnClose
        public Action<string> OnError { get; set; }
        public Action<byte[], RudpDelivery> OnMessage { get; set; }

        /* ---- CLIENT --- */

        public void StartClient()
        {
            var pending = new Pending();

            lock (_locker)
            {
                if (_state != State.Idle) return;

                _nextTickAt = 0;
                _state = State.Handshaking;
                _nonce = RudpWire.RandomUInt();
                _startedAt = RudpWire.Now();
                _lastReceiveAt = _startedAt;
                _handshakeDelay = 50;

                SendSyn();
            }

            RudpScheduler.Add(this);
            Flush(pending);
        }

        private void SendSyn()
        {
            var now = RudpWire.Now();
            var buffer = new byte[RudpWire.SynSize];

            buffer[0] = RudpWire.Handshake;
            buffer[1] = RudpWire.Syn;
            buffer[2] = RudpWire.Version;
            RudpWire.Put32(buffer, 3, _nonce);
            RudpWire.Put16(buffer, 7, (ushort)_localDatagram);

            if (_synSentAt == 0) _synSentAt = now;
            else _synRetransmitted = true;

            _nextHandshakeAt = now + _handshakeDelay;
            _handshakeDelay = Math.Min(_handshakeDelay * 2, 500);

            SendRaw(buffer, buffer.Length);
        }

        /* ---- SERVER --- */

        private void SendSynAck()
        {
            var buffer = new byte[RudpWire.SynAckSize];

            buffer[0] = RudpWire.Handshake;
            buffer[1] = RudpWire.SynAck;
            RudpWire.Put32(buffer, 2, _nonce);
            RudpWire.Put32(buffer, 6, _token);
            Buffer.BlockCopy(Guid.Parse(Id).ToByteArray(), 0, buffer, 10, 16);
            RudpWire.Put16(buffer, 26, (ushort)_localDatagram);

            _nextHandshakeAt = RudpWire.Now() + _handshakeDelay;
            _handshakeDelay = Math.Min(_handshakeDelay * 2, 500);

            SendRaw(buffer, buffer.Length);
        }

        /* ---- INTERFACE --- */

        public void Send(byte[] data, RudpDelivery messageType)
        {
            if (data == null || data.Length < 1) return;

            // every message starts with the framing header
            var framed = new byte[Framing.HeaderSize + data.Length];

            Buffer.BlockCopy(Framing.Create(data.Length), 0, framed, 0, Framing.HeaderSize);
            Buffer.BlockCopy(data, 0, framed, Framing.HeaderSize, data.Length);

            var pending = new Pending();

            lock (_locker)
            {
                if (_state != State.Connected) return;

                _nextTickAt = 0;

                if (messageType == RudpDelivery.Reliable)
                    SendReliable(framed, ref pending);
                else
                    SendDatagram(framed, messageType, ref pending);
            }

            Flush(pending);
        }

        public void Close()
        {
            var pending = new Pending();

            lock (_locker)
            {
                CloseLocked(ref pending, true, "RudpConnection closed");
            }

            Flush(pending);
        }

        public void Receive(byte[] buffer, int length)
        {
            if (length < 1) return;

            var pending = new Pending();

            lock (_locker)
            {
                if (_state == State.Closed) return;

                // a packet can create work (ack, retransmission), check this connection at the next pass
                _nextTickAt = 0;

                var type = (byte)(buffer[0] & RudpWire.TypeMask);

                if (type == RudpWire.Handshake)
                {
                    ReceiveHandshake(buffer, length, ref pending);
                }
                else if (length >= 1 + RudpWire.TokenSize && RudpWire.Get32(buffer, 1) == _token)
                {
                    // first valid packet of the client confirms the handshake
                    if (_isServer && _state == State.Handshaking) Confirm(ref pending);

                    if (_state == State.Connected)
                    {
                        _lastReceiveAt = RudpWire.Now();

                        switch (type)
                        {
                            case RudpWire.Reliable:
                                ReceiveReliable(buffer, length, ref pending);
                                break;
                            case RudpWire.Unreliable:
                            case RudpWire.Sequenced:
                                ReceiveDatagram(buffer, length, type, ref pending);
                                break;
                            case RudpWire.Ack:
                            case RudpWire.KeepAlive:
                                if (length >= RudpWire.ControlHeader) ProcessAck(buffer, 1 + RudpWire.TokenSize, ref pending);
                                break;
                            case RudpWire.Close:
                                CloseLocked(ref pending, false, null);
                                break;
                        }
                    }
                }
            }

            Flush(pending);
        }

        public void Tick(long now)
        {
            var pending = new Pending();

            lock (_locker)
            {
                switch (_state)
                {
                    case State.Handshaking:
                        TickHandshake(now, ref pending);
                        break;
                    case State.Connected:
                        TickConnected(now, ref pending);
                        break;
                }

                _nextTickAt = NextDeadline();
            }

            Flush(pending);
        }

        /* ---- HANDSHAKE --- */

        private void ReceiveHandshake(byte[] buffer, int length, ref Pending pending)
        {
            if (length < 2) return;

            var kind = buffer[1];

            if (_isServer)
            {
                if (kind == RudpWire.Syn && length >= RudpWire.SynSize && buffer[2] == RudpWire.Version)
                {
                    var nonce = RudpWire.Get32(buffer, 3);

                    if (_state == State.Idle)
                    {
                        _state = State.Handshaking;
                        _nonce = nonce;
                        _token = RudpWire.RandomUInt();
                        Id = Guid.NewGuid().ToString();
                        _startedAt = RudpWire.Now();
                        _lastReceiveAt = _startedAt;
                        _handshakeDelay = 100;
                        MaxDatagram = Negotiate(RudpWire.Get16(buffer, 7));
                        SendSynAck();
                        RudpScheduler.Add(this);
                    }
                    else if (_state == State.Handshaking && nonce == _nonce)
                    {
                        // syn again: the syn ack was lost
                        SendSynAck();
                    }
                }
                else if (kind == RudpWire.HandshakeAck && length >= RudpWire.HandshakeAckSize
                         && _state == State.Handshaking && RudpWire.Get32(buffer, 2) == _token)
                {
                    Confirm(ref pending);
                }
            }
            else if (kind == RudpWire.SynAck && length >= RudpWire.SynAckSize && _state == State.Handshaking
                     && RudpWire.Get32(buffer, 2) == _nonce)
            {
                var id = new byte[16];

                _token = RudpWire.Get32(buffer, 6);
                Buffer.BlockCopy(buffer, 10, id, 0, 16);
                Id = new Guid(id).ToString();
                MaxDatagram = Negotiate(RudpWire.Get16(buffer, 26));

                var now = RudpWire.Now();
                _lastReceiveAt = now;
                _state = State.Connected;

                // the first handshake round trip is the first rtt sample
                if (!_synRetransmitted && _synSentAt != 0) UpdateRtt(Math.Max(1, now - _synSentAt));

                var ack = new byte[RudpWire.HandshakeAckSize];
                ack[0] = RudpWire.Handshake;
                ack[1] = RudpWire.HandshakeAck;
                RudpWire.Put32(ack, 2, _token);
                SendRaw(ack, ack.Length);

                pending.Opened = true;
            }
        }

        // both peers use the smaller datagram, a bigger one would be dropped by the peer that can't hold it
        private int Negotiate(int peer)
        {
            return Math.Min(_localDatagram, Math.Max(RudpWire.MinDatagram, peer));
        }

        private void Confirm(ref Pending pending)
        {
            _state = State.Connected;
            _lastReceiveAt = RudpWire.Now();
            pending.Opened = true;
        }

        // earliest time this connection needs a tick, 0 means keep ticking
        private long NextDeadline()
        {
            switch (_state)
            {
                case State.Handshaking:
                    return Math.Min(_startedAt + HandshakeTimeout + 1, _nextHandshakeAt);

                case State.Connected:
                    // data in flight or waiting for the window needs the retransmission timer and the pump
                    if (_out.Count > _head || _assemblies.Count > 0) return 0;

                    var next = Math.Min(_lastReceiveAt + NoResponseTimeout + 1,
                        _lastSendAt + Math.Min(1000, NoResponseTimeout / 3));

                    return _ackDueAt != 0 ? Math.Min(next, _ackDueAt) : next;

                default:
                    return 0;
            }
        }

        private void TickHandshake(long now, ref Pending pending)
        {
            if (now - _startedAt > HandshakeTimeout)
            {
                CloseLocked(ref pending, false, $"RudpConnection closed, Handshake Timeout: {HandshakeTimeout}ms");
                return;
            }

            if (now < _nextHandshakeAt) return;

            if (_isServer) SendSynAck();
            else SendSyn();
        }

        /* ---- SEND --- */

        private void SendReliable(byte[] data, ref Pending pending)
        {
            // queue without limit would use all the memory
            if (_pendingBytes + data.Length > MaxPendingBytes)
            {
                Violation(ref pending, "Send queue is full");
                return;
            }

            var size = MaxDatagram - RudpWire.ReliableHeader;
            var count = (data.Length + size - 1) / size;

            for (var index = 0; index < count; index++)
            {
                var offset = index * size;
                var length = Math.Min(size, data.Length - offset);
                var buffer = new byte[RudpWire.ReliableHeader + length];
                var flags = RudpWire.Reliable;

                if (index == 0) flags |= RudpWire.First;
                if (index == count - 1) flags |= RudpWire.Last;

                buffer[0] = flags;
                RudpWire.Put32(buffer, 1, _token);
                RudpWire.Put32(buffer, 5, _nextSeq);
                Buffer.BlockCopy(data, offset, buffer, RudpWire.ReliableHeader, length);

                _out.Add(new Outgoing { Data = buffer, Seq = _nextSeq, PayloadSize = length });
                _pendingBytes += length;
                _nextSeq++;
            }

            Pump();
        }

        // send queued packets while the window allows it
        private void Pump()
        {
            // window in packets, limited by the bytes in flight (a burst above this overflows buffers)
            var window = Math.Min((int)_cwnd, Math.Max(4, MaxInFlightBytes / MaxDatagram));

            while (_sent < _out.Count)
            {
                if (_sent - _head - _sackedCount >= window) break;

                Transmit(_out[_sent], false);
                _sent++;
                _sentEnd++;
            }
        }

        private void Transmit(Outgoing outgoing, bool retransmit)
        {
            outgoing.SentAt = RudpWire.Now();

            if (retransmit) outgoing.Retransmits++;

            // every transmission carries the current ack
            WriteAck(outgoing.Data, 1 + RudpWire.TokenSize + 4);
            SendRaw(outgoing.Data, outgoing.Data.Length);

            _unackedReceived = 0;
            _ackDueAt = 0;
        }

        private void SendDatagram(byte[] data, RudpDelivery messageType, ref Pending pending)
        {
            var isSequenced = messageType == RudpDelivery.Sequenced;
            var header = 1 + RudpWire.TokenSize + (isSequenced ? 4 : 0);
            var type = isSequenced ? RudpWire.Sequenced : RudpWire.Unreliable;
            var seq = isSequenced ? ++_sendSequenced : 0;

            if (header + data.Length <= MaxDatagram)
            {
                var buffer = new byte[header + data.Length];

                buffer[0] = type;
                RudpWire.Put32(buffer, 1, _token);
                if (isSequenced) RudpWire.Put32(buffer, 5, seq);
                Buffer.BlockCopy(data, 0, buffer, header, data.Length);

                SendRaw(buffer, buffer.Length);
                return;
            }

            // message bigger than a datagram: send fragments, the receiver drops the message if any is lost
            var size = MaxDatagram - header - RudpWire.FragmentSize;
            var count = (data.Length + size - 1) / size;

            if (count > byte.MaxValue)
            {
                Logger.Singleton.Submit($"[RudpConnection] Message too big for {messageType}, ignored. Id: {Id}");
                pending.Error = $"Message too big for {messageType}";
                return;
            }

            var id = _nextMessageId++;

            for (var index = 0; index < count; index++)
            {
                var offset = index * size;
                var length = Math.Min(size, data.Length - offset);
                var buffer = new byte[header + RudpWire.FragmentSize + length];

                buffer[0] = (byte)(type | RudpWire.Fragment);
                RudpWire.Put32(buffer, 1, _token);
                if (isSequenced) RudpWire.Put32(buffer, 5, seq);
                RudpWire.Put16(buffer, header, id);
                buffer[header + 2] = (byte)index;
                buffer[header + 3] = (byte)count;
                Buffer.BlockCopy(data, offset, buffer, header + RudpWire.FragmentSize, length);

                SendRaw(buffer, buffer.Length);
            }
        }

        private void SendRaw(byte[] buffer, int length)
        {
            try
            {
                _lastSendAt = RudpWire.Now();

                if (_isServer) _socket.SendTo(buffer, 0, length, SocketFlags.None, _remote);
                else _socket.Send(buffer, 0, length, SocketFlags.None);
            }
            catch (ObjectDisposedException)
            {
                // socket closed, the connection is closing
            }
            catch (SocketException)
            {
                // datagram dropped by the system (buffer full, icmp error), the protocol recovers
            }
        }

        private void SendControl(byte type)
        {
            _control[0] = type;
            RudpWire.Put32(_control, 1, _token);
            WriteAck(_control, 1 + RudpWire.TokenSize);
            SendRaw(_control, _control.Length);

            _unackedReceived = 0;
            _ackDueAt = 0;
        }

        private void WriteAck(byte[] buffer, int offset)
        {
            ulong sack = 0;

            RudpWire.Put32(buffer, offset, _nextExpected);

            if (_reorder.Count > 0)
                for (var i = 0; i < 64; i++)
                    if (_reorder.ContainsKey(_nextExpected + 1 + (uint)i))
                        sack |= 1UL << i;

            RudpWire.Put64(buffer, offset + 4, sack);
        }

        /* ---- RECEIVE --- */

        private void ReceiveReliable(byte[] buffer, int length, ref Pending pending)
        {
            if (length < RudpWire.ReliableHeader) return;

            var seq = RudpWire.Get32(buffer, 5);

            ProcessAck(buffer, 9, ref pending);

            if (_state != State.Connected) return;

            var distance = (int)(seq - _nextExpected);

            // duplicate: the peer missed our ack
            if (distance < 0)
            {
                SendControl(RudpWire.Ack);
                return;
            }

            if (distance >= MaxReorder) return;

            if (distance == 0)
            {
                AcceptFragment(buffer[0], buffer, RudpWire.ReliableHeader, length - RudpWire.ReliableHeader, ref pending);
                _nextExpected++;

                while (_state == State.Connected && _reorder.Count > 0
                                                 && _reorder.TryGetValue(_nextExpected, out var next))
                {
                    _reorder.Remove(_nextExpected);
                    AcceptFragment(next.Flags, next.Data, 0, next.Data.Length, ref pending);
                    _nextExpected++;
                }

                if (_state != State.Connected) return;

                _unackedReceived++;

                if (_reorder.Count > 0 || _unackedReceived >= 2) SendControl(RudpWire.Ack);
                else if (_ackDueAt == 0) _ackDueAt = RudpWire.Now() + AckDelay;
            }
            else
            {
                if (!_reorder.ContainsKey(seq))
                {
                    var data = new byte[length - RudpWire.ReliableHeader];

                    Buffer.BlockCopy(buffer, RudpWire.ReliableHeader, data, 0, data.Length);
                    _reorder[seq] = new Fragment { Flags = buffer[0], Data = data };
                }

                // out of order: tell the sender now (sack), it triggers the fast retransmit
                SendControl(RudpWire.Ack);
            }
        }

        private void AcceptFragment(byte flags, byte[] buffer, int offset, int length, ref Pending pending)
        {
            var isFirst = (flags & RudpWire.First) != 0;
            var isLast = (flags & RudpWire.Last) != 0;

            if (isFirst)
            {
                // the header gives the size before any allocation
                if (!Framing.TryReadHeader(buffer, offset, length, MaxMessageSize, out var size))
                {
                    Violation(ref pending, "Invalid message header or message above the limit");
                    return;
                }

                _assemblyData = new byte[size];
                _assemblyPosition = 0;
                offset += Framing.HeaderSize;
                length -= Framing.HeaderSize;
            }

            // fragment without the first one
            if (_assemblyData == null) return;

            if (length > _assemblyData.Length - _assemblyPosition)
            {
                Violation(ref pending, "Message bigger than its header");
                return;
            }

            Buffer.BlockCopy(buffer, offset, _assemblyData, _assemblyPosition, length);
            _assemblyPosition += length;

            if (!isLast) return;

            if (_assemblyPosition != _assemblyData.Length)
            {
                Violation(ref pending, "Message smaller than its header");
                return;
            }

            var data = _assemblyData;

            _assemblyData = null;
            pending.Add(data, RudpDelivery.Reliable);
        }

        private void ReceiveDatagram(byte[] buffer, int length, byte type, ref Pending pending)
        {
            var isSequenced = type == RudpWire.Sequenced;
            var header = 1 + RudpWire.TokenSize + (isSequenced ? 4 : 0);
            var isFragment = (buffer[0] & RudpWire.Fragment) != 0;
            var messageType = isSequenced ? RudpDelivery.Sequenced : RudpDelivery.Unreliable;

            if (length < header + (isFragment ? RudpWire.FragmentSize : 0) + 1) return;

            var seq = isSequenced ? RudpWire.Get32(buffer, 5) : 0;

            if (!isFragment)
            {
                var data = new byte[length - header];

                Buffer.BlockCopy(buffer, header, data, 0, data.Length);
                DeliverDatagram(data, messageType, seq, ref pending);
                return;
            }

            var id = RudpWire.Get16(buffer, header);
            var index = buffer[header + 2];
            var count = buffer[header + 3];
            var offset = header + RudpWire.FragmentSize;

            if (count < 2 || index >= count) return;

            if (!_assemblies.TryGetValue(id, out var assembly))
            {
                if (_assemblies.Count >= MaxAssemblies) DropOldestAssembly();

                assembly = new Assembly
                {
                    Parts = new byte[count][],
                    Deadline = RudpWire.Now() + DatagramLifetime,
                    Seq = seq
                };
                _assemblies[id] = assembly;
            }

            if (assembly.Parts.Length != count || assembly.Parts[index] != null) return;

            var part = new byte[length - offset];

            Buffer.BlockCopy(buffer, offset, part, 0, part.Length);
            assembly.Parts[index] = part;
            assembly.Received++;
            assembly.Length += part.Length;

            if (assembly.Received < count) return;

            var data2 = new byte[assembly.Length];
            var position = 0;

            foreach (var item in assembly.Parts)
            {
                Buffer.BlockCopy(item, 0, data2, position, item.Length);
                position += item.Length;
            }

            _assemblies.Remove(id);
            DeliverDatagram(data2, messageType, assembly.Seq, ref pending);
        }

        private void DeliverDatagram(byte[] data, RudpDelivery messageType, uint seq, ref Pending pending)
        {
            // header must describe the message
            if (!Framing.TryReadHeader(data, 0, data.Length, MaxMessageSize, out var size) ||
                size != data.Length - Framing.HeaderSize)
            {
                Violation(ref pending, "Invalid message header or message above the limit");
                return;
            }

            if (messageType == RudpDelivery.Sequenced)
            {
                // old message: a newer one was already delivered
                if (_hasSequenced && !RudpWire.IsNewer(seq, _lastSequenced)) return;

                _hasSequenced = true;
                _lastSequenced = seq;
            }

            var payload = new byte[size];

            Buffer.BlockCopy(data, Framing.HeaderSize, payload, 0, payload.Length);
            pending.Add(payload, messageType);
        }

        private void DropOldestAssembly()
        {
            ushort oldest = 0;
            var deadline = long.MaxValue;

            foreach (var pair in _assemblies)
                if (pair.Value.Deadline < deadline)
                {
                    deadline = pair.Value.Deadline;
                    oldest = pair.Key;
                }

            _assemblies.Remove(oldest);
        }

        /* ---- ACK, RTT AND CONGESTION --- */

        private void ProcessAck(byte[] buffer, int offset, ref Pending pending)
        {
            var ack = RudpWire.Get32(buffer, offset);
            var sack = RudpWire.Get64(buffer, offset + 4);
            var now = RudpWire.Now();
            var acked = 0;
            long sample = -1;

            // ack of something never sent
            if (RudpWire.IsNewer(ack, _sentEnd)) return;

            // cumulative: everything below ack
            while (_head < _sent && RudpWire.IsNewer(ack, _out[_head].Seq))
            {
                var item = _out[_head];

                if (item.Sacked) _sackedCount--;
                else acked++;

                if (item.Retransmits == 0 && !item.Sacked) sample = now - item.SentAt;

                _pendingBytes -= item.PayloadSize;
                _out[_head] = null;
                _head++;
            }

            // selective: bitmap after ack
            var highest = 0u;
            var hasHighest = false;

            if (sack != 0 && _head < _sent)
            {
                var first = _out[_head].Seq;

                for (var i = 0; i < 64; i++)
                {
                    if ((sack & (1UL << i)) == 0) continue;

                    var seq = ack + 1 + (uint)i;

                    if (!RudpWire.IsNewer(_sentEnd, seq)) break;

                    var index = _head + (int)(seq - first);

                    if (index < _head || index >= _sent) continue;

                    var item = _out[index];

                    if (!item.Sacked)
                    {
                        item.Sacked = true;
                        _sackedCount++;
                        acked++;

                        if (item.Retransmits == 0) sample = now - item.SentAt;
                    }

                    highest = seq;
                    hasHighest = true;
                }
            }

            if (sample >= 0) UpdateRtt(Math.Max(1, sample));

            // fast retransmit: three packets after the missing one were received
            if (hasHighest)
            {
                var lost = false;

                for (var i = _head; i < _sent; i++)
                {
                    var item = _out[i];

                    if (item.Sacked || item.FastRetransmitted) continue;
                    if (!RudpWire.IsNewer(highest, item.Seq + DuplicateThreshold - 1)) break;

                    item.FastRetransmitted = true;
                    Transmit(item, true);
                    lost = true;
                }

                if (lost && !_inRecovery)
                {
                    _ssthresh = Math.Max(_cwnd * 0.7, 4);
                    _cwnd = _ssthresh;
                    _inRecovery = true;
                    _recoveryPoint = _sentEnd;
                }
            }

            if (_inRecovery && !RudpWire.IsNewer(_recoveryPoint, ack)) _inRecovery = false;

            if (acked > 0 && !_inRecovery)
            {
                // slow start, then congestion avoidance
                if (_cwnd < _ssthresh) _cwnd += acked;
                else _cwnd += acked / _cwnd;

                if (_cwnd > MaxWindow) _cwnd = MaxWindow;
            }

            if (_head > 128 && _head * 2 > _out.Count)
            {
                _out.RemoveRange(0, _head);
                _sent -= _head;
                _head = 0;
            }

            if (acked > 0) Pump();
        }

        // rfc 6298
        private void UpdateRtt(long sample)
        {
            if (!_hasRtt)
            {
                _srtt = sample;
                _rttvar = sample / 2d;
                _hasRtt = true;
            }
            else
            {
                _rttvar = 0.75 * _rttvar + 0.25 * Math.Abs(_srtt - sample);
                _srtt = 0.875 * _srtt + 0.125 * sample;
            }

            _rto = Math.Min(MaxRto, Math.Max(MinRto, (long)(_srtt + Math.Max(4, 4 * _rttvar))));
        }

        /* ---- TIMERS --- */

        private void TickConnected(long now, ref Pending pending)
        {
            if (now - _lastReceiveAt > NoResponseTimeout)
            {
                Logger.Singleton.Submit($"RudpConnection Close by Timeout (No Response), Id: {Id}");
                CloseLocked(ref pending, false, null);
                return;
            }

            if (_ackDueAt != 0 && now >= _ackDueAt) SendControl(RudpWire.Ack);

            RetransmitExpired(now, ref pending);

            if (_state != State.Connected) return;

            // idle connection
            if (now - _lastSendAt >= Math.Min(1000, NoResponseTimeout / 3)) SendControl(RudpWire.KeepAlive);

            if (_assemblies.Count > 0) DropExpiredAssemblies(now);
        }

        private void RetransmitExpired(long now, ref Pending pending)
        {
            var expired = false;
            var count = 0;

            for (var i = _head; i < _sent && count < 32; i++)
            {
                var item = _out[i];

                if (item.Sacked) continue;
                if (now - item.SentAt < _rto) break;

                if (item.Retransmits >= MaxRetransmits)
                {
                    Logger.Singleton.Submit($"RudpConnection Close by too many retransmissions, Id: {Id}");
                    CloseLocked(ref pending, false, null);
                    return;
                }

                Transmit(item, true);
                expired = true;
                count++;
            }

            if (!expired) return;

            // timeout: back off and restart the window
            _rto = Math.Min(MaxRto, _rto * 2);
            _ssthresh = Math.Max(_cwnd / 2, 4);
            _cwnd = 4;
            _inRecovery = false;
        }

        private void DropExpiredAssemblies(long now)
        {
            List<ushort> expired = null;

            foreach (var pair in _assemblies)
                if (pair.Value.Deadline <= now)
                {
                    if (expired == null) expired = new List<ushort>();
                    expired.Add(pair.Key);
                }

            if (expired == null) return;

            foreach (var id in expired) _assemblies.Remove(id);
        }

        /* ---- CLOSE --- */

        // OnError, then the connection closes
        private void Violation(ref Pending pending, string message)
        {
            Logger.Singleton.Submit($"[RudpConnection] {message}, connection closed. Id: {Id}");

            pending.Error = message;
            CloseLocked(ref pending, true, null);
        }

        private void CloseLocked(ref Pending pending, bool notify, string failMessage)
        {
            if (_state == State.Closed) return;

            var wasConnected = _state == State.Connected;

            // server side handshake: the client may already be connected (its ack is on the way)
            var peerMayBeOpen = wasConnected || (_isServer && _state == State.Handshaking);

            _state = State.Closed;

            // best effort, a lost close is detected by timeout
            if (notify && peerMayBeOpen)
            {
                _control[0] = RudpWire.Close;
                RudpWire.Put32(_control, 1, _token);

                for (var i = 0; i < 5; i++) SendRaw(_control, 1 + RudpWire.TokenSize);
            }

            RudpScheduler.Remove(this);

            _out.Clear();
            _reorder.Clear();
            _assemblies.Clear();
            _assemblyData = null;
            _head = _sent = _sackedCount = 0;
            _pendingBytes = 0;

            if (wasConnected) pending.Closed = true;
            else pending.Fail = failMessage ?? "RudpConnection closed";
        }

        private void Flush(Pending pending)
        {
            try
            {
                if (pending.Opened) OnOpen?.Invoke();

                if (pending.Messages != null)
                    foreach (var message in pending.Messages)
                        OnMessage?.Invoke(message.Key, message.Value);

                if (pending.Error != null) OnError?.Invoke(pending.Error);

                if (pending.Fail != null) OnOpenFail?.Invoke(pending.Fail);

                if (pending.Closed) OnClose?.Invoke();
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }
        }
    }
}
