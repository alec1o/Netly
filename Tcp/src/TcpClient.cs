using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Zenet.Core;

namespace Zenet.Tcp
{
    /// <summary>
    /// "TcpClient" is a class that facilitates the creation and interaction with TCP client sockets
    /// </summary>
    public class TcpClient : IClient
    {
        #region Private

        private Socket _socket { get; set; }
        private ZHost _host { get; set; }

        private readonly bool _isServer;
        private bool _closeEmited { get; set; }
        private bool _tryOpen { get; set; }
        private bool _tryClose { get; set; }

        private EventHandler _OnOpen { get; set; }
        private EventHandler<Exception> _OnError { get; set; }
        private EventHandler _OnClose { get; set; }
        private EventHandler<byte[]> _OnData { get; set; }
        private EventHandler<(string name, byte[] data)> _OnEvent { get; set; }
        private EventHandler<Socket> _OnBeforeOpen { get; set; }
        private EventHandler<Socket> _OnAfterOpen { get; set; }

        #endregion

        #region Public

        /// <summary>
        /// Target Socket
        /// </summary>
        public Socket Socket => _socket;

        /// <summary>
        /// The connection destination "End Point"
        /// </summary>
        public ZHost Host => _host;

        /// <summary>
        /// Returns "true" if the connection to the server is open
        /// </summary>
        public bool Opened => VerifyOpened();

        /// <summary>
        /// Connection Id is very useful when the server is using it to compare and verify client instances
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        #endregion

        #region Init

        /// <summary>
        /// Creating an instance of TcpClient
        /// </summary>
        public TcpClient()
        {
            _host = new ZHost(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Creating an instance of 'TcpClient' to emulate a tcp client on the server
        /// </summary>
        /// <param name="socket"></param>
        internal TcpClient(Socket socket)
        {
            _host = new ZHost(socket.RemoteEndPoint);
            _socket = socket;
            _isServer = true;
        }

        /// <summary>
        /// To start the client tasks. Only when booted as client emulator
        /// </summary>
        internal void InitServer()
        {
            _OnOpen?.Invoke(this, null);
            Receive();
        }

        #endregion

        #region Func

        private bool VerifyOpened()
        {
            try
            {
                if (_socket == null || !_socket.Connected) return false;
                return !(Socket.Poll(5000, SelectMode.SelectRead) && Socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        private void Receive()
        {
            ZAsync.Execute(() =>
            {
                byte[] buffer = new byte[1024 * 8];

                while (Opened)
                {
                    try
                    {
                        int _size = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                        if (_size <= 0) continue;

                        byte[] _data = new byte[_size];
                        Buffer.BlockCopy(buffer, 0, _data, 0, _size);

                        (string name, byte[] data) _event = ZEvent.Verify(_data);

                        if (string.IsNullOrEmpty(_event.name))
                        {
                            _OnData?.Invoke(this, _data);
                        }
                        else
                        {
                            _OnEvent?.Invoke(this, _event);
                        }
                    }
                    catch
                    {

                    }
                }

                if (!_tryClose || !_closeEmited)
                {
                    _closeEmited = true;
                    _OnClose?.Invoke(this, null);
                }
            });
        }

        #endregion

        #region Remote

        /// <summary>
        /// Opens a connection to the server if the connection is closed
        /// </summary>
        /// <param name="host">The connection destination "End Point"</param>
        public void Open(ZHost host)
        {
            if (Opened || _tryOpen || _isServer) return;

            _tryOpen = true;

            ZAsync.Execute(() =>
            {
                Exception _exception = null;

                try
                {
                    _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);

                    _OnBeforeOpen?.Invoke(this, _socket);

                    _socket.Connect(host.EndPoint);

                    _host = host;

                    _closeEmited = false;

                    _OnAfterOpen?.Invoke(this, _socket);
                }
                catch (Exception e)
                {
                    _exception = e;
                }
                finally
                {
                    _tryOpen = false;

                    if (_exception == null)
                    {
                        Receive();
                        _OnOpen?.Invoke(this, null);
                    }
                    else
                    {
                        _OnError?.Invoke(this, _exception);
                    }
                }
            });
        }

        /// <summary>
        /// Closes a connection to the server if the connection is open
        /// </summary>
        public void Close()
        {
            if (!Opened || _tryClose) return;

            _tryClose = true;

            _socket.Shutdown(SocketShutdown.Both);

            ZAsync.Execute(() =>
            {
                try
                {
                    _socket?.Close();
                    _socket?.Dispose();
                }
                catch
                {
                    _socket = null;
                }
                finally
                {
                    _tryClose = false;

                    if (!_closeEmited)
                    {
                        _closeEmited = true;
                        _OnClose?.Invoke(this, null);
                    }
                }
            });
        }

        #endregion

        #region Send

        /// <summary>
        /// Sends raw data to the server
        /// </summary>
        /// <param name="data">The date to be published</param>
        public void ToData(byte[] data)
        {
            if (data == null || data.Length <= 0) return;

            try
            {
                _socket.Send(data, 0, data.Length, SocketFlags.None);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Sends a formatted data "event" to the server
        /// </summary>
        /// <param name="name">Event name "subscription"</param>
        /// <param name="data">The date to be published</param>
        public void ToEvent(string name, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(name) || data == null || data.Length <= 0) return;

            byte[] _event = ZEvent.Create(name, data);

            ToData(_event);
        }

        #endregion

        #region Events        

        /// <summary>
        /// It is called "invoked callback" when it has received a connection opening
        /// </summary>
        /// <param name="callback">The callback invoked when the event received</param>
        public void OnOpen(Action callback)
        {
            _OnOpen += (_, __) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        /// <summary>
        /// It is called "invoked callback" when the connection opening attempt fails
        /// </summary>
        /// <param name="callback">The callback invoked when the event received</param>
        public void OnError(Action<Exception> callback)
        {
            _OnError += (_, exception) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke(exception);
                });
            };
        }

        /// <summary>
        /// Is called "invoked callback" the connection is closed. being the client or server responsible for the closure
        /// </summary>
        /// <param name="callback">The callback invoked when the event received</param>
        public void OnClose(Action callback)
        {
            _OnClose += (_, __) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        /// <summary>
        /// It is called The "invoked callback" the socket receiving raw data from the server
        /// </summary>
        /// <code>
        /// TcpClient client = new TcpClient();
        /// client.OnData((byte[] data) =>
        /// {
        ///     // convert data to string
        ///     string message = ZEncoding.GetString(data);
        ///     Console.WriteLine(message);
        /// });
        /// </code>
        /// <param name="callback">The callback invoked when the event received</param>
        public void OnData(Action<byte[]> callback)
        {
            _OnData += (_, data) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke(data);
                });
            };
        }

        /// <summary>
        /// It is called The "invoked callback" the socket receiving a formatted "event" from the server
        /// </summary>
        /// <param name="callback">The callback invoked when the event received</param>
        public void OnEvent(Action<string, byte[]> callback)
        {
            _OnEvent += (_, container) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke(container.name, container.data);
                });
            };
        }

        /// <summary>
        /// Is called "invoked callback" before connection open attempt is finished
        /// </summary>
        /// <param name="callback">The callback invoked when the event received</param>
        public void BeforeOpen(Action<Socket> callback)
        {
            _OnBeforeOpen += (_, socket) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        /// <summary>
        /// It is called "invoked callback" after connection opening is completed and succeeded
        /// </summary>
        /// <param name="callback">The callback invoked when the event received</param>
        public void AfterOpen(Action<Socket> callback)
        {
            _OnAfterOpen += (_, socket) =>
            {
                ZCallback.Execute(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        #endregion
    }
}
