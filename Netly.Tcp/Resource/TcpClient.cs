using Netly.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Netly.Tcp
{
    /// <summary>
    /// TCP: Client
    /// </summary>
    public class TcpClient : ITcpClient
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
        /// Returns true if using encryption like SSL, TLS
        /// </summary>
        public bool IsEncrypted { get; private set; }

        /// <summary>
        /// Returns true if socket is connected
        /// </summary>
        public bool Opened { get => Connected(); }

        #endregion

        #region Private

        private SslStream _sslStream;
        private NetworkStream _stream;

        private Socket _socket;

        private bool _isServer;
        private bool _tryOpen;
        private bool _tryClose;
        private bool _invokeClose;

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
        public TcpClient()
        {
            Id = string.Empty;
            Host = new Host(IPAddress.Any, 0);
            _isServer = false;
            _socket = new Socket(Host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        internal TcpClient(string id, Socket socket)
        {
            Id = id;
            _isServer = true;
            _socket = socket;
            _socket.NoDelay = true;
            Host = new Host(socket.RemoteEndPoint ?? new IPEndPoint(IPAddress.Any, 0));
        }

        internal void InitServer()
        {
            _OnOpen?.Invoke(this, null);
            Receive();
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
                    _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _socket.NoDelay = true;
                    _OnBeforeOpen?.Invoke(this, _socket);

                    _socket.Connect(host.EndPoint);

                    Host = host;

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

            _tryClose = true;

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

        /// <summary>
        /// Use to send raw data, using bytes
        /// </summary>
        /// <param name="value"></param>
        public void ToData(byte[] value)
        {
            if (_invokeClose) return;

            try
            {
                if (IsEncrypted)
                {
                    _sslStream.Write(value, 0, value.Length);
                }
                else
                {
                    _stream.Write(value, 0, value.Length);
                }
            }
            catch
            {

            }
        }
        
        /// <summary>
        /// Use to send raw data, using string
        /// </summary>
        /// <param name="value"></param>
        public void ToData(string value)
        {
            this.ToData(Encode.GetBytes(value, Encode.Mode.UTF8));
        }

        /// <summary>
        /// Use to send a certain event, using bytes
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">data</param>
        public void ToEvent(string name, byte[] value)
        {
            ToData(Events.Create(name, value));
        }
        
        /// <summary>
        /// Use to send a certain event, using string
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">data</param>
        public void ToEvent(string name, string value)
        {
            ToData(Events.Create(name, Encode.GetBytes(value, Encode.Mode.UTF8)));
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

            _stream = new NetworkStream(_socket);

            if (IsEncrypted)
            {
                _sslStream = new SslStream(_stream);
                throw new NotImplementedException(nameof(IsEncrypted));
            }
            else
            {
                Async.SafePool(() =>
                {
                    while (!_invokeClose)
                    {
                        try
                        {
                            length = _stream.Read(buffer, 0, buffer.Length);

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
        }

        /// <summary>
        /// Use to make use of encryption
        /// </summary>
        /// <param name="value">Use encryption?</param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public void UseEncryption(bool value)
        {
            if (Opened)
            {
                throw new Exception("Error, you can't add encryption configuration to an open socket");
            }

            throw new NotImplementedException(nameof(UseEncryption));

            // IsEncrypted = value;
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

        #region Static

        private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        #endregion
    }
}
