using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Byter;
using Netly.Core;

namespace Netly
{
    /// <summary>
    /// Netly: TcpClient
    /// </summary>
    public class TcpClient : IClient
    {
        public Host Host { get; private set; }
        public bool IsOpened { get => Connected(); }

        internal string uid;
        private Socket _socket;

        private bool _closed;
        private bool _closing;
        private bool _connecting;
        private bool _serverMode;

        private EventHandler<Socket> onModifyHandler;
        private EventHandler<byte[]> onDataHandler;
        private EventHandler<Exception> onErrorHandler;
        private EventHandler onOpenHandler, onCloseHandler;
        private EventHandler<(string name, byte[] data)> onEventHandler;


        public TcpClient()
        {
            _serverMode = false;
        }

        internal TcpClient(string id, Socket socket)
        {
            uid = id;
            _socket = socket;
            _serverMode = true;
            Host = new Host(socket.RemoteEndPoint);
        }


        internal void InitServerMode()
        {
            onOpenHandler?.Invoke(null, null);
            Receive();
        }

        public void Open(Host host)
        {
            if (IsOpened || _connecting || _closing || _serverMode) return;

            _connecting = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    _socket.NoDelay = true;

                    onModifyHandler?.Invoke(null, _socket);

                    _socket.Connect(host.EndPoint);

                    Host = host;

                    _closed = false;

                    onOpenHandler?.Invoke(null, null);

                    Receive();
                }
                catch (Exception e)
                {
                    onErrorHandler?.Invoke(null, e);
                }

                _connecting = false;
            });
        }

        public void Close()
        {
            if (!IsOpened || _connecting || _closing) return;

            _closing = true;

            _socket.Shutdown(SocketShutdown.Both);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _socket.Close();
                    _socket.Dispose();
                }
                finally
                {
                    _socket = null;

                    if (!_closed)
                    {
                        _closed = true;
                        onCloseHandler?.Invoke(null, null);
                    }
                }

                _closing = false;
            });
        }

        public void ToData(byte[] value)
        {
            if (_closed) return;

            byte[] buffer = BufferParser.SetPrefix(ref value);

            _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void ToEvent(string name, byte[] value)
        {
            ToData(MessageParser.Create(name, value));
        }

        public void ToData(string value)
        {
            this.ToData(NE.GetBytes(value, NE.Mode.UTF8));
        }

        public void ToEvent(string name, string value)
        {
            this.ToEvent(name, NE.GetBytes(value, NE.Mode.UTF8));
        }

        private bool Connected()
        {
            try
            {
                if (_socket == null || !_socket.Connected) return false;
                const int timeout = 5000;
                return !(_socket.Poll(timeout, SelectMode.SelectRead) && _socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        private void Receive()
        {
            int length = 0;
            byte[] buffer = new byte[1024 * 8];

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_closed)
                {
                    try
                    {
                        length = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                        if (length <= 0)
                        {
                            if (IsOpened is false) break;
                            continue;
                        }

                        byte[] data = new byte[length];
                        Array.Copy(buffer, 0, data, 0, data.Length);

                        List<byte[]> messages = BufferParser.GetMessages(ref data);

                        foreach (byte[] message in messages)
                        {
                            (string name, byte[] buffer) content = MessageParser.Verify(message);

                            if (content.buffer == null)
                                onDataHandler?.Invoke(null, data);
                            else
                                onEventHandler?.Invoke(null, (content.name, content.buffer));
                        }
                    }
                    catch
                    {
                        if (length <= 0)
                        {
                            if (IsOpened is false) break;
                            continue;
                        }
                    }
                }

                if (!_closing || !_closed)
                {
                    _closed = true;
                    onCloseHandler?.Invoke(null, null);
                }
            });

        }

        public void OnOpen(Action callback)
        {
            onOpenHandler += (_, __) =>
            {
                MainThread.Add(() => callback?.Invoke());
            };
        }

        public void OnClose(Action callback)
        {
            onCloseHandler += (_, __) =>
            {
                MainThread.Add(() => callback?.Invoke());
            };
        }

        public void OnError(Action<Exception> callback)
        {
            onErrorHandler += (_, exception) =>
            {
                MainThread.Add(() => callback?.Invoke(exception));
            };
        }

        public void OnData(Action<byte[]> callback)
        {
            onDataHandler += (_, buffer) =>
            {
                MainThread.Add(() => callback?.Invoke(buffer));
            };
        }

        public void OnEvent(Action<string, byte[]> callback)
        {
            onEventHandler += (_, content) =>
            {
                MainThread.Add(() => callback?.Invoke(content.name, content.data));
            };
        }

        public void OnModify(Action<Socket> callback)
        {
            onModifyHandler += (_, socket) =>
            {
                MainThread.Add(() => callback?.Invoke(socket));
            };
        }
    }
}
