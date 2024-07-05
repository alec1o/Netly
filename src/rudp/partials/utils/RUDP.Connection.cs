using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Byter;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Connection
        {
            private readonly bool _isServer;
            private readonly Host _host;
            private readonly Socket _socket;
            public bool IsOpened { get; private set; }
            private readonly Dictionary<uint, byte[]> _localReliableQueue;
            private readonly Dictionary<uint, byte[]> _receivedQueue;
            private readonly NetlyEnvironment.MessageFraming _framing;
            private uint _localReliableId, _localSequencedId;
            private bool _isOpeningOrClosing;
            public Action OnOpen, OnClose;
            public Action<string> OnOpenFail;
            public Action<bool> OnServer;
            public Action<byte[], MessageType> OnData;
            public Action<string, byte[], MessageType> OnEvent;
            private readonly object _localReliableIdLocker, _localSequencedIdLocker, _localReliableQueueLocker;
            public int HandshakeTimeout, NoResponseTimeout;
            private const byte PingByte = 0, SynByte = 16, AckByte = 32, SynAckByte = 64, FinByte = 128;

            private class Package
            {
                public byte Type { get; set; }
                public byte[] Data { get; set; }
                public MessageType MessageType => (MessageType)Type;
            }

            private Connection()
            {
                var logger = NetlyEnvironment.Logger;
                string @class = nameof(Connection);

                _isServer = false;
                _isOpeningOrClosing = false;
                _framing = new NetlyEnvironment.MessageFraming();

                _localReliableQueue = new Dictionary<uint, byte[]>();
                _localReliableQueueLocker = new object();

                _localReliableId = 0;
                _localReliableIdLocker = new object();

                _localSequencedId = 0;
                _localSequencedIdLocker = new object();

                _receivedQueue = new Dictionary<uint, byte[]>();

                HandshakeTimeout = 5000; // 5s
                NoResponseTimeout = 10000; // 10s

                OnOpen = () => logger.Create($"{@class} -> {nameof(OnOpen)}");
                OnClose = () => logger.Create($"{@class} -> {nameof(OnClose)}");
                OnOpenFail = e => logger.Create($"{@class} -> {nameof(OnOpenFail)}: {e}");
                OnData = (d, t) => logger.Create($"{@class} -> {nameof(OnData)}: {d.GetString()} ({t})");
                OnEvent = (n, d, t) => logger.Create($"{@class} -> {nameof(OnData)} ({n}): {d.GetString()} ({t})");
            }

            public Connection(Host host, Socket socket, bool isServer) : this()
            {
                _host = host ?? throw new NullReferenceException(nameof(host));
                _socket = socket ?? throw new NullReferenceException(nameof(socket));
                _isServer = isServer;
            }

            public Task Open()
            {
                if (_isOpeningOrClosing || IsOpened) return Task.CompletedTask;
                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
                    //
                });
            }

            public Task Close()
            {
                if (_isOpeningOrClosing || !IsOpened) return Task.CompletedTask;
                _isOpeningOrClosing = true;

                return Task.Run(() =>
                {
//
                });
            }

            public void Send(ref byte[] bytes, MessageType messageType)
            {
                if (bytes == null || bytes.Length <= 0) return;

                try
                {
                    var primitive = new Primitive();

                    switch (messageType)
                    {
                        case MessageType.Unreliable:
                        {
                            primitive.Add.Byte((byte)messageType);
                            break;
                        }
                        case MessageType.Sequenced:
                        {
                            uint messageId = GetNewSequencedId();
                            primitive.Add.Byte((byte)messageType);
                            primitive.Add.UInt(messageId);
                            break;
                        }
                        case MessageType.Reliable:
                        {
                            uint messageId = GetNewReliableId();
                            primitive.Add.Byte((byte)messageType);
                            primitive.Add.UInt(messageId);

                            // this message just will be removed from _localReliableQueue when receive message Ack 
                            lock (_localReliableQueueLocker)
                            {
                                _localReliableQueue.Add(messageId, bytes);
                            }

                            break;
                        }
                        default:
                        {
                            throw new InvalidOperationException($"{messageType} is not implemented.");
                        }
                    }

                    primitive.Add.Bytes(bytes);

                    var data = primitive.GetBytes();
                    
                    primitive.Reset();

                    if (_isServer)
                    {
                        // this way of send just work on windows and linux, except macOS (maybe iOs)
                        _socket?.BeginSendTo
                        (
                            data,
                            0,
                            data.Length,
                            SocketFlags.None,
                            _host.EndPoint,
                            null,
                            null
                        );
                    }
                    else
                    {
                        // this way of send just work on windows and linux, include macOS and iOs
                        _socket?.BeginSend
                        (
                            data,
                            0,
                            data.Length,
                            SocketFlags.None,
                            null,
                            null
                        );
                    }
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void InjectBuffer(byte[] buffer)
            {
            }

            private uint GetNewReliableId()
            {
                lock (_localReliableIdLocker)
                {
                    _localReliableId++;
                    return _localReliableId;
                }
            }

            private uint GetNewSequencedId()
            {
                lock (_localSequencedIdLocker)
                {
                    _localSequencedId++;
                    return _localSequencedId;
                }
            }

            private bool IsReceived(uint id)
            {
                lock (_localReliableQueueLocker)
                {
                    return _localReliableQueue.ContainsKey(id);
                }
            }
        }
    }
}