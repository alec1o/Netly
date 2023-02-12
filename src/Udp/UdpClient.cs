using Netly;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Netly.Core;

namespace Netly
{
    /// <summary>
    /// Netly: UdpClient
    /// </summary>
    public class UdpClient : IClient
    {
        #region Var

        internal string Id;

        /// <summary>
        /// Endpoint
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        /// Returns true if socket is connected
        /// </summary>
        public bool IsOpened { get => Connected(); }

        private Socket _socket;

        private bool _isServer;
        private bool _tryOpen;
        private bool _tryClose;
        private bool _invokeClose;
        private bool _opened;

        private EventHandler _OnOpen;
        private EventHandler _OnClose;
        private EventHandler<Exception> _OnError;
        private EventHandler<byte[]> _OnData;
        private EventHandler<(string name, byte[] data)> _OnEvent;

        private EventHandler<Socket> _OnModify;

        #endregion

        /// <summary>
        /// Creating instance
        /// </summary>
        public UdpClient()
        {
            Id = string.Empty;
            Host = new Host(IPAddress.Any, 0);
            _isServer = false;
            _socket = new Socket(Host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        internal UdpClient(string id, ref Socket socket, Host host)
        {
            Id = id;
            _isServer = true;
            _socket = socket;
            Host = host;
        }

        internal void InitServer()
        {
            _opened = true;
            _OnOpen?.Invoke(this, null);
        }

        /// <summary>
        /// Is called, executes action before socket connect
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnModify(Action<Socket> callback)
        {
            _OnModify += (sender, socket) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        #region Init

        /// <summary>
        /// Use to open connection
        /// </summary>
        /// <param name="host">Endpoint</param>
        public void Open(Host host)
        {
            if (IsOpened || _tryOpen || _tryClose || _isServer) return;

            _tryOpen = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _OnModify?.Invoke(this, _socket);

                    _socket.Connect(host.EndPoint);

                    Host = host;

                    _opened = true;

                    _invokeClose = false;

                    _OnOpen?.Invoke(this, EventArgs.Empty);

                    Receive();
                }
                catch (Exception e)
                {
                    _OnError?.Invoke(this, e);
                }

                _tryOpen = false;
            });
        }

        /// <summary>
        /// Use to close connection
        /// </summary>
        public void Close()
        {
            if (!IsOpened || _tryOpen || _tryClose) return;

            if (_isServer)
            {
                _OnClose?.Invoke(this, null);

                return;
            }

            _tryClose = true;
            _opened = false;

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

                    if (!_invokeClose)
                    {
                        _invokeClose = true;
                        _OnClose?.Invoke(this, EventArgs.Empty);
                    }
                }

                _tryClose = false;
            });
        }

        // TODO.
        /// <summary>
        /// Use to send raw data, using bytes
        /// </summary>
        /// <param name="value"></param>
        public void ToData(byte[] value)
        {
            if (_invokeClose) return;

            try
            {
                _socket.SendTo(value, value.Length, SocketFlags.None, Host.IPEndPoint);
            }
            catch
            {

            }
        }

        // TODO.
        /// <summary>
        /// Use to send raw data, using string
        /// </summary>
        /// <param name="value"></param>
        public void ToData(string value)
        {
            this.ToData(NE.GetBytes(value, NE.Mode.UTF8));
        }

        /// <summary>
        /// Use to send a certain event, using bytes
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">data</param>
        public void ToEvent(string name, byte[] value)
        {
            ToData(MessageParser.Create(name, value));
        }

        /// <summary>
        /// Use to send a certain event, using string
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">data</param>
        public void ToEvent(string name, string value)
        {
            ToData(MessageParser.Create(name, NE.GetBytes(value, NE.Mode.UTF8)));
        }

        private bool Connected()
        {
            if (_socket == null) return false;

            return _opened;
        }

        // TODO.
        private void Receive()
        {
            int length = 0;
            byte[] buffer = new byte[1024 * 8];
            EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_invokeClose)
                {
                    try
                    {
                        length = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                        if (length <= 0)
                        {
                            if (IsOpened)
                            {
                                continue;
                            }

                            break;
                        }

                        byte[] data = new byte[length];

                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);

                        var events = MessageParser.Verify(data);

                        if (string.IsNullOrEmpty(events.name))
                        {
                            _OnData?.Invoke(this, data);
                        }
                        else
                        {
                            _OnEvent?.Invoke(this, (events.name, events.data));
                        }
                    }
                    catch
                    {
                        if (IsOpened)
                        {
                            continue;
                        }

                        break;
                    }
                }

                if (!_tryClose || !_invokeClose)
                {
                    _invokeClose = true;
                    _OnClose?.Invoke(this, EventArgs.Empty);
                }
            });

        }

        internal void AddData(byte[] data)
        {
            var events = MessageParser.Verify(data);

            if (data.Length <= 0)
            {
                _opened = false;
                _OnClose?.Invoke(this, null);
            }
            else
            {
                if (string.IsNullOrEmpty(events.name))
                {
                    _OnData?.Invoke(this, data);
                }
                else
                {
                    _OnEvent?.Invoke(this, (events.name, events.data));
                }

                // TODO: refresh timeout
            }
        }


        #endregion

        #region Events

        /// <summary>
        /// Execute the callback, when: the connection is opened
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnOpen(Action callback)
        {
            _OnOpen += (sender, args) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: the connection is closed
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnClose(Action callback)
        {
            _OnClose += (sender, args) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: the connection cannot be opened
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnError(Action<Exception> callback)
        {
            _OnError += (sender, exception) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(exception);
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: when the connection receives raw data
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnData(Action<byte[]> callback)
        {
            _OnData += (sender, data) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(data);
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: when the connection receives event data
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnEvent(Action<string, byte[]> callback)
        {
            _OnEvent += (sender, result) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(result.name, result.data);
                });
            };
        }

        #endregion
    }
}
