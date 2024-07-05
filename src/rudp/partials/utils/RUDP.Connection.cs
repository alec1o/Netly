using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Byter;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Connection
        {
            public bool IsOpened { get; private set; }
            public int HandshakeTimeout { get; set; }
            public int NoResponseTimeout { get; set; }

            private readonly bool _isServer;
            private readonly Host _host;
            private readonly Socket _socket;
            private readonly List<byte[]> _injectQueue;
            private readonly NetlyEnvironment.MessageFraming _framing;

            private readonly Dictionary<uint, byte[]>
                _localReliableQueue,
                _remoteUnorderedReliableQueue;


            private uint
                _localReliableId,
                _localSequencedId,
                _remoteReliableId,
                _remoteSequencedId;

            private bool
                _isOpeningOrClosing,
                _isUpdating;

            private readonly object
                _injectQueueLocker,
                _localReliableIdLocker,
                _localSequencedIdLocker,
                _localReliableQueueLocker;

            public Action
                OnOpen,
                OnClose;

            public Action<string> OnOpenFail;
            public Action<bool> OnServer;
            public Action<byte[], MessageType> OnData;
            public Action<string, byte[], MessageType> OnEvent;

            private const byte
                PingByte = 0,
                SynByte = 16,
                AckByte = 32,
                SynAckByte = 64,
                FinByte = 128,
                DataAckByte = 255;

            /// <summary>
            /// PI * 999<br/>
            /// What is the meaning of Juice Wrld's hand sign? 999 represents taking any bad situation or struggle and turning it into something positive and using it to push yourself forward. Nine, nine, nine represents turning a bad situation or struggle into something positive and pushing yourself forward.
            /// </summary>
            private const float InternalActionKey = (float)Math.PI * 999;

            private Connection()
            {
                var logger = NetlyEnvironment.Logger;
                string @class = nameof(Connection);

                _isServer = false;
                _isUpdating = false;
                _isOpeningOrClosing = false;
                _framing = new NetlyEnvironment.MessageFraming();
                _remoteUnorderedReliableQueue = new Dictionary<uint, byte[]>();

                _injectQueue = new List<byte[]>();
                _injectQueueLocker = new object();

                _localReliableQueue = new Dictionary<uint, byte[]>();
                _localReliableQueueLocker = new object();

                _localReliableId = 0;
                _localReliableIdLocker = new object();

                _localSequencedId = 0;
                _localSequencedIdLocker = new object();

                _remoteReliableId = 0;
                _remoteSequencedId = 0;

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
                    InitUpdate();
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

            private void ResendReliablePackage(uint id, byte[] bytes)
            {
                var primitive = new Primitive();
                primitive.Add.UInt(id);
                primitive.Add.Bytes(bytes);

                var buffer = primitive.GetBytes();
                primitive.Reset();

                SendRaw(ref buffer);
            }

            private void SendRaw(ref byte[] bytes)
            {
                if (_isServer)
                {
                    // this way of send just work on windows and linux, except macOS (maybe iOs)
                    _socket?.BeginSendTo
                    (
                        bytes,
                        0,
                        bytes.Length,
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
                        bytes,
                        0,
                        bytes.Length,
                        SocketFlags.None,
                        null,
                        null
                    );
                }
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

                    SendRaw(ref data);
                }
                catch (Exception e)
                {
                    NetlyEnvironment.Logger.Create(e);
                }
            }

            public void InjectBuffer(byte[] buffer)
            {
                lock (_injectQueueLocker)
                {
                    _injectQueue.Add(buffer);
                }
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

            private void InitUpdate()
            {
                Task task = new Task(Update, TaskCreationOptions.LongRunning);
                task.Start();
            }

            private void Update()
            {
                if (_isUpdating) return;
                _isUpdating = true;

                while (_isUpdating)
                {
                    try
                    {
                        UpdateInjection();
                        UpdateReliable();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                }

                _isUpdating = false;
            }

            private void UpdateInjection()
            {
                byte[][] buffers;

                lock (_injectQueueLocker)
                {
                    if (_injectQueue.Count <= 0) return;

                    buffers = _injectQueue.ToArray();
                }

                foreach (var buffer in buffers)
                {
                    var primitive = new Primitive(buffer);

                    var messageType = (MessageType)primitive.Get.Byte();

                    if (!primitive.IsValid) continue; // ignored data

                    switch (messageType)
                    {
                        case MessageType.Unreliable:
                        {
                            byte[] data = primitive.GetBytes();

                            if (!primitive.IsValid) break;
                            bool isInternalAction = false;

                            // detect if this package is Ack, DataAck, Fin...
                            /* tag (1+1) (2); ActionKey: 4+1 (5); = 7; */
                            /* 5 is uint ack package data: 4+1 (5); = 7 + 5 (12); NOTE: (5) this is max overhead */
                            if (data.Length >= 7 && data.Length <= 7 + 5)
                            {
                                var myPrimitive = new Primitive(data);

                                // read rudp tag
                                byte tag = myPrimitive.Get.Byte();

                                // check if rudp tag exist
                                if (myPrimitive.IsValid)
                                {
                                    // check is the flag
                                    switch (tag)
                                    {
                                        case DataAckByte:
                                        {
                                            uint dataAckId = myPrimitive.Get.UInt();
                                            var key = myPrimitive.Get.Float();

                                            if (myPrimitive.IsValid && InternalActionKey.Equals(key))
                                            {
                                                isInternalAction = true;

                                                // remove package from ack queue

                                                lock (_localReliableQueueLocker)
                                                {
                                                    if (_localReliableQueue.ContainsKey(dataAckId))
                                                    {
                                                        // remove from local queue.
                                                        // this mean the endpoint answered ack (received) to this package id
                                                        _localReliableQueue.Remove(dataAckId);
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }

                                myPrimitive.Reset();
                            }

                            // dispatch
                            if (!isInternalAction)
                            {
                                OnData?.Invoke(data, MessageType.Unreliable);
                            }

                            break;
                        }
                        case MessageType.Sequenced:
                        {
                            uint id = primitive.Get.UInt();

                            if (!primitive.IsValid) break;

                            byte[] data = primitive.GetBytes();

                            if (!primitive.IsValid) break;

                            // check latest sequenced
                            if (id > _remoteSequencedId)
                            {
                                // update latest sequenced
                                _remoteSequencedId = id;
                                // dispatch
                                OnData?.Invoke(data, MessageType.Sequenced);
                            }

                            break;
                        }
                        case MessageType.Reliable:
                        {
                            uint id = primitive.Get.UInt();

                            if (!primitive.IsValid) break;

                            byte[] data = primitive.GetBytes();

                            if (!primitive.IsValid) break;

                            // detect if message already received and answer the ack
                            if (id <= _remoteReliableId)
                            {
                                SendPackageAck(id);
                                break;
                            }

                            void UpdateLatestReliableId(uint latestId) => _remoteReliableId = latestId;

                            void DispatchLatestReliablePackage(ref byte[] latestPackage) =>
                                OnData?.Invoke(latestPackage, MessageType.Reliable);

                            // detect if the message is sequenced or save on unordered queue to be sequenced in future
                            if (id == (_remoteReliableId + 1)) // is current
                            {
                                // dispatch
                                DispatchLatestReliablePackage(ref data);

                                // dispatch current package
                                // update latest reliable ordered
                                UpdateLatestReliableId(id);

                                // try dispatch unordered package
                                if (_remoteUnorderedReliableQueue.Count > 0)
                                {
                                    uint GetNextId()
                                    {
                                        return _remoteReliableId + 1;
                                    }

                                    while (_remoteUnorderedReliableQueue.ContainsKey(GetNextId()))
                                    {
                                        var nextId = GetNextId();

                                        // get current package from unordered queue
                                        var unorderedData = _remoteUnorderedReliableQueue[nextId];

                                        // dispatch
                                        DispatchLatestReliablePackage(ref unorderedData);

                                        // dispatch current package
                                        // update latest reliable ordered
                                        UpdateLatestReliableId(nextId);

                                        // delete the unordered package because was ordered correctly
                                        _remoteUnorderedReliableQueue.Remove(nextId);
                                    }
                                }

                                // answer ack
                                SendPackageAck(id);
                            }
                            else
                            {
                                // save this package to be dispatch on ordered way
                                if (_remoteUnorderedReliableQueue.ContainsKey(id))
                                {
                                    // package already received but not dispatched because is unordered, answer the ack
                                    SendPackageAck(id);
                                }
                                else
                                {
                                    // set received unordered data to queue
                                    _remoteUnorderedReliableQueue.Add(id, data);

                                    // answer ack
                                    SendPackageAck(id);
                                }
                            }

                            break;
                        }
                    }

                    primitive.Reset();
                }
            }

            private void UpdateReliable()
            {
                lock (_localReliableQueueLocker)
                {
                    if (_localReliableQueue.Count > 0)
                    {
                        foreach (var package in _localReliableQueue)
                        {
                            // resend package because ack isn't received
                            // NOTE: when ack received this package will be removed from this current list
                            ResendReliablePackage(package.Key, package.Value);
                        }
                    }
                }
            }

            private void SendPackageAck(uint id)
            {
                var primitive = new Primitive();
                primitive.Add.Byte(DataAckByte);
                primitive.Add.UInt(id);
                primitive.Add.Float(InternalActionKey);
                var bytes = primitive.GetBytes();
                Send(ref bytes, MessageType.Unreliable);
            }
        }
    }
}