using Netly.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Netly.Tcp
{
    public class TcpClient : ITcpClient
    {
        #region Var

        #region Public

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Host Host { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public bool IsEncrypted { get; private set; }
        
        /// <summary>
        /// 
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
        /// 
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
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnBeforeOpen(Action<Socket> callback)
        {
            _OnBeforeOpen += (sender, socket) => callback?.Invoke(socket);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnAfterOpen(Action<Socket> callback)
        {
            _OnAfterOpen += (sender, socket) => callback?.Invoke(socket);
        }

        #endregion

        #region Init

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        public void Open(Host host)
        {
            if (Opened || _tryOpen || _tryClose || _isServer) return;

            _tryOpen = true;

            Async.SafePool(() =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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
        /// 
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
        /// 
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
                    _stream = new NetworkStream(_socket);
                    _stream.Write(value, 0, value.Length);
                }
            }
            catch
            {
                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void ToEvent(string name, byte[] value)
        {
            ToData(Events.Create(name, value));
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
        /// 
        /// </summary>
        /// <param name="value"></param>
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
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnOpen(Action callback)
        {
            _OnOpen += (sender, args) => callback?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnClose(Action callback)
        {
            _OnClose += (sender, args) => callback?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnError(Action<Exception> callback)
        {
            _OnError += (sender, exception) => callback?.Invoke(exception);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnData(Action<byte[]> callback)
        {
            _OnData += (sender, data) => callback?.Invoke(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public void OnEvent(Action<string, byte[]> callback)
        {
            _OnEvent += (sender, result) => callback?.Invoke(result.name, result.data);
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