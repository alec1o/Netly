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
            private readonly Dictionary<uint, bool> _database;
            private readonly Dictionary<uint, byte[]> _receivedQueue;
            private readonly NetlyEnvironment.MessageFraming _framing;
            private uint _receivedId, _sendId;
            private bool _isOpeningOrClosing;
            public Action OnOpen, OnClose;
            public Action<string> OnOpenFail;
            public Action<bool> OnServer;
            public Action<byte[], MessageType> OnData;
            public Action<string, byte[], MessageType> OnEvent;
            private readonly object _sendIdLocker, _databaseLocker;
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
                _database = new Dictionary<uint, bool>();
                _receivedQueue = new Dictionary<uint, byte[]>();
                _receivedId = 0;
                _sendId = 0;
                _framing = new NetlyEnvironment.MessageFraming();
                _isOpeningOrClosing = false;
                _sendIdLocker = new object();
                _databaseLocker = new object();
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
            }

            public void InjectBuffer(byte[] buffer)
            {
            }

            private uint GetNextId()
            {
                lock (_sendIdLocker)
                {
                    _sendId++;
                    return _sendId;
                }
            }

            private bool IsReceived(uint id)
            {
                lock (_databaseLocker)
                {
                    return _database.ContainsKey(id);
                }
            }
        }
    }
}