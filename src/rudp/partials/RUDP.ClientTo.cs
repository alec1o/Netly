﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientTo : IRUDP.ClientTo
            {
                public Host Host { get; private set; }
                public bool IsOpened => IsConnected();
                private ClientOn On => _client._on;
                private readonly Client _client;
                private Socket _socket;
                private bool _isOpeningOrClosing, _isClosed;
                private readonly bool _isServer;
                private int _openTimeout, _resendDelay;
                private MessageId _messageId;
                private readonly object _nextIdLock = new object();
                private readonly object _queueLock = new object();
                private readonly List<(uint id, byte[] data)> _reliableQueueList;
                private readonly List<(uint id, byte[] data)> _reliableOrderedQueueList;

                private struct Config
                {
                    public const byte PingBuffer = 0;
                    public const byte OpenBuffer = 128;
                    public const byte CloseBuffer = 255;
                    public const byte OpenRepeat = 32;
                    public const byte CloseRepeat = 32;
                }

                private class Package
                {
                    public uint Id { get; set; }
                    public byte[] Data { get; set; }
                    public sbyte Type { get; set; }
                }

                private class MessageId
                {
                    public uint Reliable { get; set; }
                    public uint ReliableUnordered { get; set; }
                    public uint Unreliable { get; }
                    public uint UnreliableOrdered { get; set; }
                }


                private ClientTo()
                {
                    _isOpeningOrClosing = false;
                    _isClosed = true;
                    _socket = null;
                    _client = null;
                    _isServer = false;
                    _resendDelay = 10;
                    _openTimeout = 3000;
                    _reliableQueueList = new List<(uint id, byte[] data)>();
                    _reliableOrderedQueueList = new List<(uint id, byte[] data)>();
                    Host = Host.Default;
                }

                public ClientTo(Client client)
                {
                    _client = client;
                }

                public Task Open(Host host)
                {
                    if (!IsOpened || _isOpeningOrClosing || _isServer) return Task.CompletedTask;
                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            On.OnModify?.Invoke(null, _socket);

                            _socket.Connect(host.Address, host.Port);

                            // create connection data
                            var data = new byte[] { Config.OpenBuffer };

                            // send connected data
                            for (int i = 0; i < Config.OpenRepeat; i++)
                            {
                                SendRaw(null, ref data);
                            }

                            // waiting for connection data response
                            List<byte> ackList = new List<byte>();

                            try
                            {
                                Task.Run(() =>
                                {
                                    while (ackList.Count < Config.OpenRepeat)
                                    {
                                        var buffer = new byte[1024];
                                        var endpoint = Host.EndPoint;

                                        var size = _socket.ReceiveFrom
                                        (
                                            buffer,
                                            0,
                                            buffer.Length,
                                            SocketFlags.None,
                                            ref endpoint
                                        );

                                        if (size == 1)
                                        {
                                            ackList.Add(buffer[0]);
                                        }
                                    }
                                }).Wait(_openTimeout);
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }

                            // check if connection already opened
                            int feeds = 0;

                            foreach (var ack in ackList)
                            {
                                if (ack == Config.OpenBuffer)
                                {
                                    feeds++;
                                }
                            }

                            if (feeds <= 0)
                            {
                                throw new Exception
                                (
                                    $"A connection attempt failed because the connected party did not properly respond after a period of time ({_openTimeout}ms), or established connection failed because connected host has failed to respond"
                                );
                            }

                            InitReceiver();

                            // connected successful
                            Host = new Host(_socket.RemoteEndPoint);

                            lock (_nextIdLock)
                            {
                                _messageId = new MessageId();
                            }

                            _isClosed = false;

                            On.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            _isClosed = true;
                            _socket = null;
                            On.OnError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpeningOrClosing = false;
                        }
                    });
                }

                public Task Close()
                {
                    if (IsOpened || _isOpeningOrClosing) return Task.CompletedTask;
                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        if (IsOpened)
                        {
                            try
                            {
                                var data = new[] { Config.CloseBuffer };

                                for (var i = 0; i < Config.CloseRepeat; i++)
                                {
                                    SendRaw(Host, ref data);
                                }
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }
                        }

                        if (!_isServer)
                            try
                            {
                                _socket?.Shutdown(SocketShutdown.Both);
                                _socket?.Close();
                                _socket?.Dispose();
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }
                            finally
                            {
                                _socket = null;
                            }

                        _isOpeningOrClosing = false;
                        _isClosed = true;

                        On.OnClose?.Invoke(null, null);
                    });
                }

                public void Data(byte[] data, MessageType messageType)
                {
                    if (data == null || data.Length <= 0) return;

                    Send(data, messageType);
                }

                public void Data(string data, MessageType messageType)
                {
                    if (data == null || data.Length <= 0) return;

                    Send(data.GetBytes(), messageType);
                }

                public void Data(string data, MessageType messageType, Encoding encoding)
                {
                    if (data == null || data.Length <= 0) return;

                    Send(data.GetBytes(encoding), messageType);
                }

                public void Event(string name, byte[] data, MessageType messageType)
                {
                    if (data == null || data.Length <= 0) return;

                    Send(NetlyEnvironment.EventManager.Create(name, data), messageType);
                }

                public void Event(string name, string data, MessageType messageType)
                {
                    if (data == null || data.Length <= 0 || name == null || name.Length <= 0) return;

                    Send(NetlyEnvironment.EventManager.Create(name, data.GetBytes()), messageType);
                }

                public void Event(string name, string data, MessageType messageType, Encoding encoding)
                {
                    if (data == null || data.Length <= 0 || name == null || name.Length <= 0) return;

                    Send(NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)), messageType);
                }


                private void Send(byte[] bytes, MessageType messageType)
                {
                    Send(Host, ref bytes, messageType);
                }

                private void Send(Host host, ref byte[] bytes, MessageType messageType)
                {
                    var packageId = GetNextId(messageType);
                    var package = new Package
                    {
                        Type = (sbyte)messageType,
                        Data = bytes,
                        Id = packageId
                    };

                    GetBufferFromPackage(ref package, out var buffer);

                    if (buffer.Length <= 0) return;

                    switch (messageType)
                    {
                        case MessageType.Reliable:
                        {
                            _reliableQueueList.Add((packageId, buffer));
                            break;
                        }
                        case MessageType.ReliableUnordered:
                        {
                            _reliableOrderedQueueList.Add((packageId, buffer));
                            break;
                        }
                    }

                    SendRaw(host, ref buffer);
                }

                private void SendRaw(Host host, ref byte[] bytes)
                {
                    if (!IsOpened) return;

                    try
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
                                host.EndPoint,
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
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                }

                private bool IsConnected()
                {
                    if (_socket == null || _isClosed) return false;
                    return true;
                }

                public void SetOpenTimeout(int value)
                {
                    if (IsOpened)
                        throw new Exception
                        (
                            $"Isn't possible use `{nameof(SetOpenTimeout)}` while socket is already connected."
                        );

                    if (value <= 0)
                        throw new Exception
                        (
                            $"Isn't possible use {nameof(SetOpenTimeout)} with a `0` or `negative` value"
                        );

                    _openTimeout = value;
                }

                public void SetResendDelay(int value)
                {
                    if (IsOpened)
                        throw new Exception
                        (
                            $"Isn't possible use `{nameof(SetResendDelay)}` while socket is already connected."
                        );

                    if (value <= 0)
                        throw new Exception
                        (
                            $"Isn't possible use {nameof(SetResendDelay)} with a `0` or `negative` value"
                        );

                    _resendDelay = value;
                }

                public int GetOpenTimeout()
                {
                    return _openTimeout;
                }

                public int GetResendDelay()
                {
                    return _resendDelay;
                }

                private uint GetNextId(MessageType messageType)
                {
                    lock (_nextIdLock)
                    {
                        switch (messageType)
                        {
                            case MessageType.Reliable:
                            {
                                _messageId.Reliable++;
                                return _messageId.Reliable;
                            }
                            case MessageType.ReliableUnordered:
                            {
                                _messageId.ReliableUnordered++;
                                return _messageId.ReliableUnordered;
                            }
                            case MessageType.Unreliable:
                            {
                                return _messageId.Unreliable;
                            }
                            case MessageType.UnreliableOrdered:
                            {
                                _messageId.UnreliableOrdered++;
                                return _messageId.UnreliableOrdered;
                            }
                            default:
                            {
                                throw new InvalidOperationException(messageType.ToString());
                            }
                        }
                    }
                }

                private void GetBufferFromPackage(ref Package package, out byte[] buffer)
                {
                    var primitive = new Primitive();

                    primitive.Add.Class(package);

                    buffer = primitive.GetBytes();

                    primitive.Reset();
                }

                private void PushResult(ref byte[] bytes, MessageType messageType)
                {
                    (string name, byte[] buffer) content = NetlyEnvironment.EventManager.Verify(bytes);

                    if (content.buffer == null)
                        On.OnData?.Invoke(null, (bytes, messageType));
                    else
                        On.OnEvent?.Invoke(null, (content.name, content.buffer, messageType));
                }

                private void InitReceiver()
                {
                    var endpoint = Host.EndPoint;

                    var buffer = new byte
                    [
                        // Maximum/Default receive buffer length.
                        (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)
                    ];

                    ReceiverUpdate();

                    Task.Run(() =>
                    {
                        while (IsOpened)
                        {
                            ReliableQueueTask();
                            Thread.Sleep(_resendDelay);
                        }
                    });

                    void ReceiverUpdate()
                    {
                        if (!IsOpened)
                        {
                            Close();
                            return;
                        }

                        _socket.BeginReceiveFrom
                        (
                            buffer,
                            0,
                            buffer.Length,
                            SocketFlags.None,
                            ref endpoint,
                            ReceiveCallback,
                            null
                        );
                    }

                    void ReceiveCallback(IAsyncResult result)
                    {
                        try
                        {
                            var size = _socket.EndReceiveFrom(result, ref endpoint);

                            if (size <= 0)
                            {
                                if (IsOpened)
                                    ReceiverUpdate();
                                else
                                    Close();
                                return;
                            }

                            byte[] data = null;

                            if (size == 1)
                            {
                                // optimize 1 byte (ping, connection ack)
                                data = new[] { buffer[0] };
                            }
                            else if (size == 5)
                            {
                                // optimize 5 bytes (reliable ack)
                                data = new[] { buffer[0], buffer[1], buffer[2], buffer[3], buffer[4] };
                            }
                            else
                            {
                                // read any data
                                data = new byte[size];
                                Array.Copy(buffer, 0, data, 0, data.Length);
                            }

                            ProcessBuffer(ref data);

                            ReceiverUpdate();
                        }
                        catch (Exception e)
                        {
                            NetlyEnvironment.Logger.Create(e);
                            Close();
                        }
                    }
                }

                private void ProcessBuffer(ref byte[] data)
                {
                    if (data.Length == 1)
                    {
                        return;
                    }

                    Primitive primitive = new Primitive(data);
                    var package = primitive.Get.Class<Package>();

                    // invalid package
                    if (!primitive.IsValid)
                    {
                        primitive.Reset();
                        return;
                    }

                    MessageType messageType = (MessageType)package.Type;

                    switch (messageType)
                    {
                        case MessageType.Unreliable:
                        {
                            var bytes = package.Data;
                            PushResult(ref bytes, MessageType.Unreliable);
                            break;
                        }
                        case MessageType.UnreliableOrdered:
                        {
                            // TODO:
                            break;
                        }
                        case MessageType.Reliable:
                        {
                            // TODO:
                            break;
                        }
                        case MessageType.ReliableUnordered:
                        {
                            // TODO:
                            break;
                        }
                        default:
                        {
                            throw new Exception($"{messageType} isn't supported.");
                        }
                    }
                }

                private void ReliableQueueTask()
                {
                    try
                    {
                        if (_reliableQueueList.Count > 0)
                        {
                            (uint id, byte[] data)[] array;

                            lock (_queueLock)
                            {
                                array = _reliableQueueList.ToArray();
                            }

                            if (array.Length > 0)
                            {
                                foreach (var value in array)
                                {
                                    var package = value;
                                    SendRaw(Host, ref package.data);
                                }
                            }
                        }

                        if (_reliableOrderedQueueList.Count > 0)
                        {
                            (uint id, byte[] data)[] array;

                            lock (_queueLock)
                            {
                                array = _reliableOrderedQueueList.ToArray();
                            }

                            if (array.Length > 0)
                            {
                                foreach (var value in array)
                                {
                                    var package = value;
                                    SendRaw(Host, ref package.data);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create(e);
                    }
                }
            }
        }
    }
}