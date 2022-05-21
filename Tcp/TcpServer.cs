using System;
using System.Collections.Generic;
using Zenet.Network;
using System.Net.Sockets;
using System.Net;

namespace Zenet.Tcp
{
    public class TcpServer : IServer
    {
        #region Private

        private Socket _socket { get; set; }
        private Host _host { get; set; }

        private bool _tryOpen { get; set; }
        private bool _tryClose { get; set; }
        private bool _opened { get; set; }

        private EventHandler _OnOpen { get; set; }
        private EventHandler<Exception> _OnError { get; set; }
        private EventHandler _OnClose { get; set; }
        private EventHandler<object> _OnEnter { get; set; }
        private EventHandler<object> _OnExit { get; set; }
        private EventHandler<(object client, byte[] data)> _OnData { get; set; }
        private EventHandler<(object client, string name, byte[] data)> _OnEvent { get; set; }

        #endregion

        #region Public

        public Socket Socket => _socket;

        public Host Host => _host;

        public bool Opened => VerifyOpened();

        public List<object> Clients { get; private set; }

        #endregion

        #region Init

        public TcpServer()
        {
            _host = new Host(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
            Clients = new List<object>();
        }


        #endregion

        #region Func

        private bool VerifyOpened()
        {
            if (_socket == null) return false;

            return _opened;
        }

        private void BeginAccept()
        {
            try
            {
                _socket.BeginAccept(EndAccept, null);
            }
            catch
            {
                if (Opened)
                {
                    BeginAccept();
                }
            }
        }

        private void EndAccept(IAsyncResult result)
        {
            Socket _clientSocket = _socket.EndAccept(result);

            TcpClient _client = new TcpClient(_clientSocket);            
            
            Clients.Add(_client);

            _client.OnOpen(() =>
            {
                _OnEnter?.Invoke(this, _client);
            });

            _client.OnClose(() =>
            {
                _OnExit?.Invoke(this, _client);
            });

            _client.OnData((data) =>
            {
                _OnData?.Invoke(this, (_client, data));
            });

            _client.OnEvent((name, data) =>
            {
                _OnEvent?.Invoke(this, (_client, name, data));
            });

            _client.InitServer();

            BeginAccept();
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

                    _socket.Bind(host.EndPoint);
                    _socket.Listen(0);

                    _host = host;
                    _opened = true;
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
                        this.BeginAccept();
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
                    object[] clients = Clients.ToArray();

                    Clients.Clear();

                    foreach(object client in clients)
                    {
                        try
                        {
                            TcpClient _client = (TcpClient)client;
                            _client?.Close();
                        }
                        catch
                        {

                        }
                    }

                    _opened = false;
                    _tryClose = false;
                    _OnClose?.Invoke(this, null);
                }
            });
        }

        #endregion

        #region Client

        public void OnEnter(Action<object> callback)
        {
            _OnEnter += (_, client) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        public void OnExit(Action<object> callback)
        {
            _OnExit += (_, client) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        public void OnData(Action<object, byte[]> callback)
        {
            _OnData += (_, container) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(container.client, container.data);
                });
            };
        }

        public void OnEvent(Action<object, string, byte[]> callback)
        {
            _OnEvent += (_, container) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(container.client, container.name, container.data);
                });
            };
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

        #endregion        
    }
}
