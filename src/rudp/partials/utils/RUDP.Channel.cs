using System;
using System.Collections.Generic;
using System.Linq;
using Byter;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Channel
        {
            public const int UpdateDelay = 10;
            public const int ResendReliablePackageDelay = 20;
            public const int SendPingInterval = 1000 / 10;
            public static readonly byte[] PingPackageBytes = { 0 };
            public static readonly byte[] ClosePackageBytes = { 1 };

            private const int
                ResentCount = 2,
                SendAckCount = 3;

            private readonly object
                _locker = new object(),
                _reliableLocker = new object(),
                _sequencedLocker = new object();

            private readonly Primitive _primitive = new Primitive();
            private uint _receivedSequencedId, _receivedReliableId;
            private uint _sequencedId, _reliableId;
            private DateTime _pingTimer;


            public Channel(Host host)
            {
                _ = host;
                _sequencedId = 0;
                _reliableId = 0;
                _pingTimer = DateTime.UtcNow;
            }

            public Action<byte[], MessageType> OnRawData { get; set; }
            public Action OnClose { get; set; }
            public Action<byte[]> SendRaw { get; set; }
            private readonly Dictionary<uint, DataContent> ReliablePackages = new Dictionary<uint, DataContent>();

            private readonly Dictionary<uint, byte[]> ReceivedQueue = new Dictionary<uint, byte[]>();

            public void Close()
            {
                ReliablePackages.Clear();
                ReceivedQueue.Clear();
            }

            public void ToAddData(byte[] data, MessageType messageType)
            {
                if (data != null && data.Length > 0)
                    lock (_locker)
                    {
                        // adding prefix (package type)
                        _primitive.Add.Byte((byte)messageType);

                        // adding header
                        switch (messageType)
                        {
                            case MessageType.Unreliable:
                            {
                                // don't need more information
                                break;
                            }
                            case MessageType.Sequenced:
                            {
                                _sequencedId++;
                                _primitive.Add.UInt(_sequencedId);
                                break;
                            }
                            case MessageType.Reliable:
                            {
                                _reliableId++;
                                _primitive.Add.UInt(_reliableId);

                                lock (_locker)
                                {
                                    ReliablePackages.Add(_reliableId, new DataContent(_reliableId, data));
                                }

                                break;
                            }
                        }

                        _primitive.Add.Bytes(data);
                        SendRaw(_primitive.GetBytes());
                        _primitive.Reset();
                    }
            }

            public void ToUpdateReliableQueue()
            {
                lock (_locker)
                {
                    var now = DateTime.UtcNow;

                    KeyValuePair<uint, DataContent>[] packages = null;

                    if (ReliablePackages.Count > 0)
                    {
                        packages = ReliablePackages.ToArray();
                    }

                    if (packages != null)
                        foreach (var package in packages)
                            if (package.Value.TimeoutAt <= now)
                            {
                                _primitive.Add.Byte((byte)MessageType.Reliable);
                                _primitive.Add.UInt(package.Value.Id);
                                _primitive.Add.Bytes(package.Value.Data);
                                var buffer = _primitive.GetBytes();
                                _primitive.Reset();

                                for (int i = 0; i < ResentCount; i++)
                                {
                                    SendRaw(buffer);
                                }

                                package.Value.UpdateTimeout();
                            }

                    if (now > _pingTimer)
                    {
                        _pingTimer = DateTime.UtcNow.AddMilliseconds(SendPingInterval);
                        SendRaw(PingPackageBytes);
                    }
                }
            }

            public void OnReceiveRaw(ref byte[] data, Host host)
            {
                if (data == null) return;
                // Console.WriteLine($"Received: {data.Length}b [{host}]");

                // Detect ping data
                if (data.Length == 1)
                {
                    if (data[0] == PingPackageBytes[0])
                    {
                        // Ping received.
                        // Console.WriteLine($"[{host} PING...]");
                        return;
                    }

                    if (data[0] == ClosePackageBytes[0])
                    {
                        OnClose?.Invoke();
                        return;
                    }
                }

                // 7 bytes means.
                // --------------
                // 2 bytes for type: 1 byte overhead + 1 byte content
                // 5 bytes for id: 1 byte overhead + 4 byte for id (is uint)
                // ---------------
                // note: this result is based on <Byter.Primitive> package used to serialize and deserialize package
                const int contentSize = 7;
                var primitive = new Primitive(data);
                var receivedType = primitive.Get.Byte();

                // Remove data when RECEIVE ACK Package
                if (data.Length == contentSize)
                {
                    var receivedId = primitive.Get.UInt();

                    if (primitive.IsValid)
                    {
                        if (receivedType == (byte)MessageType.Reliable)
                        {
                            // Console.WriteLine($"Ack Received for ID: {receivedId}");
                            // remove received package
                            lock (_locker)
                            {
                                if (ReliablePackages.ContainsKey(receivedId)) ReliablePackages.Remove(receivedId);
                            }
                        }

                        return;
                    }
                }

                // Isn't ACK Package handle the data
                {
                    if (primitive.IsValid)
                        switch (receivedType)
                        {
                            case (byte)MessageType.Unreliable:
                            {
                                var buffer = primitive.Get.Bytes();
                                if (primitive.IsValid) OnRawData?.Invoke(buffer, MessageType.Unreliable);
                                return;
                            }
                            case (byte)MessageType.Reliable:
                            {
                                lock (_reliableLocker)
                                {
                                    var receivedId = primitive.Get.UInt();
                                    var nextDataId = 1 + _receivedReliableId;

                                    if (receivedId <= _receivedReliableId)
                                    {
                                        // answer ack: data received successful
                                        SendPackageAck(receivedId);
                                        return;
                                    }

                                    var buffer = primitive.Get.Bytes();

                                    if (primitive.IsValid)
                                    {
                                        if (nextDataId == receivedId)
                                        {
                                            _receivedReliableId = receivedId;
                                            SendPackageAck(receivedId);
                                            OnRawData?.Invoke(buffer, MessageType.Reliable);

                                            uint queryIndex = 0;

                                            while (true)
                                            {
                                                queryIndex++;
                                                var nextId = _receivedReliableId + queryIndex;
                                                var exist = ReceivedQueue.TryGetValue(nextId, out var nextData);

                                                if (!exist) break;

                                                _receivedReliableId = nextId;

                                                ReceivedQueue.Remove(nextId);

                                                OnRawData?.Invoke(nextData, MessageType.Reliable);
                                            }
                                        }
                                        else if (receivedId > nextDataId)
                                        {
                                            if (!ReceivedQueue.ContainsKey(receivedId))
                                                ReceivedQueue.Add(receivedId, buffer);
                                            SendPackageAck(receivedId);
                                        }
                                    }
                                }

                                break;
                            }
                            case (byte)MessageType.Sequenced:
                            {
                                lock (_sequencedLocker)
                                {
                                    var receivedId = primitive.Get.UInt();
                                    if (receivedId > _receivedSequencedId)
                                    {
                                        _receivedSequencedId = receivedId;
                                        var buffer = primitive.Get.Bytes();
                                        if (primitive.IsValid) OnRawData?.Invoke(buffer, MessageType.Sequenced);
                                    }
                                }

                                break;
                            }
                        }
                }
            }

            private void SendPackageAck(uint receivedId)
            {
                var primitive = new Primitive();
                primitive.Add.Byte((byte)MessageType.Reliable);
                primitive.Add.UInt(receivedId);
                var buffer = primitive.GetBytes();

                for (int i = 0; i < SendAckCount; i++)
                {
                    SendRaw(buffer);
                }

                primitive.Reset();
            }

            public static bool IsValidEntryPoint(byte[] value)
            {
                if (value.Length <= 0) return false;

                var primitive = new Primitive();
                var receivedType = primitive.Get.Byte();
                if (!primitive.IsValid || receivedType != (byte)Reliable) return false;
                var prefix = primitive.Get.Int(); // prefix
                if (prefix != Connection.HandshakeDataPrefix) return false;
                var content = primitive.Get.Int(); // content
                switch (content)
                {
                    case Connection.HandshakeData1:
                    case Connection.HandshakeData2:
                    case Connection.HandshakeData3:
                        break; // success
                    default:
                        return false;
                }

                if (!primitive.IsValid) return false;
                return true;
            }
        }
    }
}