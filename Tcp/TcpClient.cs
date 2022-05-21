using System;
using System.Net;
using System.Net.Sockets;
using Zenet.Network;
using Zenet.Package;

namespace Zenet.Tcp
{
    public class TcpClient : IClient
    {
        #region Private

        private Socket _socket { get; set; }
        private Host _host { get; set; }

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

        #endregion

        #region Init

        public TcpClient()
        {
            _host = new Host(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
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

        #endregion

        #region Remote

        public void Open(Host host)
        {
            if (Opened || _tryOpen) return;

            _tryOpen = true;

            Async.Thread(() =>
            {
                Exception _exception = null;

                try
                {
                    _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);

                    _socket.Connect(host.EndPoint);

                    _host = host;
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
                        _OnOpen?.Invoke(this, null);
                    }
                    else
                    {
                        _OnError?.Invoke(this, _exception);
                    }

                    this.Receive();
                }
            });
        }

        public void Close()
        {
            if (!Opened || _tryClose) return;

            _tryClose = true;

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
                    _OnClose?.Invoke(this, null);
                }
            });
        }

        #endregion

        #region Send

        public void ToData(byte[] data)
        {
            throw new NotImplementedException();
        }

        public void ToEvent(string name, byte[] data)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }        

        #endregion
    }
}
