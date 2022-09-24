using Netly.Core;
using System;
using System.Net;
using System.Net.Sockets;

namespace Netly.Udp
{
    /// <summary>
    /// Udp: Client
    /// </summary>
    public class UdpClient : IUdpClient
    {
        #region Var

        #region Public

        /// <summary>
        /// Client identifier
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        /// Returns true if socket is connected
        /// </summary>
        public bool Opened { get => IsOpened(); }

        #endregion

        #region Private

        private Socket _socket;

        private bool _isServer;
        private bool _tryOpen;
        private bool _tryClose;
        private bool _invokeClose;
        private bool _opened;

        #region Events

        private EventHandler _OnOpen;
        private EventHandler _OnClose;
        private EventHandler<Exception> _OnError;
        private EventHandler<byte[]> _OnData;
        private EventHandler<(string name, byte[] data)> _OnEvent;

        private EventHandler<Socket> _OnBeforeOpen;
        private EventHandler<Socket> _OnAfterOpen;

        #endregion

        #endregion

        #endregion

        #region Builder

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

        #endregion

        #region Customization Event

        /// <summary>
        /// Is called, executes action before socket connect
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnBeforeOpen(Action<Socket> callback)
        {
            _OnBeforeOpen += (sender, socket) =>
            {
                Call.Execute(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        /// <summary>
        /// Is called, executes action after socket connect
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnAfterOpen(Action<Socket> callback)
        {
            _OnAfterOpen += (sender, socket) =>
            {
                Call.Execute(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        #endregion

        #region Init

        /// <summary>
        /// Use to open connection
        /// </summary>
        /// <param name="host">Endpoint</param>
        public void Open(Host host)
        {
            if (Opened || _tryOpen || _tryClose || _isServer) return;

            _tryOpen = true;

            Async.SafePool(() =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _OnBeforeOpen?.Invoke(this, _socket);

                    _socket.Connect(host.EndPoint);

                    Host = host;

                    _opened = true;

                    _invokeClose = false;

                    _OnAfterOpen?.Invoke(this, _socket);

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
            if (!Opened || _tryOpen || _tryClose) return;

            if(_isServer)
            {
                _OnClose?.Invoke(this, null);

                return;
            }

            _tryClose = true;
            _opened = false;

            _socket.Shutdown(SocketShutdown.Both);

            Async.SafePool(() =>
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
            this.ToData(Encode.GetBytes(value));
        }

        /// <summary>
        /// Use to send a certain event
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">data</param>
        public void ToEvent(string name, byte[] value)
        {
            ToData(Events.Create(name, value));
        }

        private bool IsOpened()
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

            Async.SafePool(() =>
            {
                while (!_invokeClose)
                {
                    try
                    {
                        length = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                        if (length <= 0)
                        {
                            if (Opened)
                            {
                                continue;
                            }

                            break;
                        }

                        byte[] data = new byte[length];

                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);

                        var events = Events.Verify(data);

                        if (string.IsNullOrEmpty(events.name))
                        {
                            _OnData?.Invoke(this, data);
                        }
                        else
                        {
                            _OnEvent?.Invoke(this, (events.name, events.value));
                        }
                    }
                    catch
                    {
                        if (Opened)
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
            var events = Events.Verify(data);

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
                    _OnEvent?.Invoke(this, (events.name, events.value));
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
                Call.Execute(() =>
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
                Call.Execute(() =>
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
                Call.Execute(() =>
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
                Call.Execute(() =>
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
                Call.Execute(() =>
                {
                    callback?.Invoke(result.name, result.data);
                });
            };
        }

        #endregion
    }
}
