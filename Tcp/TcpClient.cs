using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Zenet.Network;
using Zenet.Package;

namespace Zenet.Tcp
{
    public class TcpClient : IClient
    {
        #region Private

        private Socket _socket { get; set; }
        private Host _host { get; set; }

        private readonly bool _isServer;
        private bool _closeEmited { get; set; }
        private bool _tryOpen { get; set; }
        private bool _tryClose { get; set; }

        private EventHandler _OnOpen { get; set; }
        private EventHandler<Exception> _OnError { get; set; }
        private EventHandler _OnClose { get; set; }
        private EventHandler<byte[]> _OnData { get; set; }
        private EventHandler<(string name, byte[] data)> _OnEvent { get; set; }

        #endregion

        #region Public

        public Socket Socket => _socket;

        public Host Host => _host;

        public bool Opened => VerifyOpened();

        public string Id { get; } = Guid.NewGuid().ToString();

        #endregion

        #region Init

        public TcpClient()
        {
            _host = new Host(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
        }

        internal TcpClient(Socket socket)
        {
            _host = new Host(socket.RemoteEndPoint);
            _socket = socket;
            _isServer = true;
        }

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
            Async.Thread(() =>
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

                        (string name, byte[] data) _event = Event2.Verify(_data);

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

                if(!_tryClose || !_closeEmited)
                {
                    _closeEmited = true;
                    _OnClose?.Invoke(this, null);
                }
            });
        }

        #endregion

        #region Remote

        public void Open(Host host)
        {
            if (Opened || _tryOpen || _isServer) return;

            _tryOpen = true;

            Async.Thread(() =>
            {
                Exception _exception = null;

                try
                {
                    _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);

                    _socket.Connect(host.EndPoint);

                    _host = host;

                    _closeEmited = false;
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
                        this.Receive();
                        _OnOpen?.Invoke(this, null);
                    }
                    else
                    {
                        _OnError?.Invoke(this, _exception);
                    }
                }
            });
        }

        public void Close()
        {
            if (!Opened || _tryClose) return;

            _tryClose = true;

            _socket.Shutdown(SocketShutdown.Both);

            Async.Thread(() =>
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

        public void ToEvent(string name, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(name) || data == null || data.Length <= 0) return;

            byte[] _event = Event2.Create(name, data);
            
            ToData(_event);
        }

        #endregion

        #region Events        

        public void OnOpen(Action callback)
        {
            _OnOpen += (_, __) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        public void OnError(Action<Exception> callback)
        {
            _OnError += (_, exception) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(exception);
                });
            };
        }

        public void OnClose(Action callback)
        {
            _OnClose += (_, __) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        public void OnData(Action<byte[]> callback)
        {
            _OnData += (_, data) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(data);
                });
            };
        }        

        public void OnEvent(Action<string, byte[]> callback)
        {
            _OnEvent += (_, container) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(container.name, container.data);
                });
            };
        }        

        #endregion
    }
}
