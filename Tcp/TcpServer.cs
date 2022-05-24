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
        private EventHandler<TcpClient> _OnEnter { get; set; }
        private EventHandler<TcpClient> _OnExit { get; set; }
        private EventHandler<(TcpClient client, byte[] data)> _OnData { get; set; }
        private EventHandler<(TcpClient client, string name, byte[] data)> _OnEvent { get; set; }
        private EventHandler<Socket> _OnBeforeOpen { get; set; }
        private EventHandler<Socket> _OnAfterOpen { get; set; }

        #endregion

        #region Public

        public Socket Socket => _socket;

        public Host Host => _host;

        public bool Opened => VerifyOpened();

        public List<TcpClient> Clients { get; private set; }

        #endregion

        #region Init

        public TcpServer()
        {            
            _host = new Host(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
            Clients = new List<TcpClient>();
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
            Socket _clientSocket;

            try
            {
                _clientSocket = _socket.EndAccept(result);
            }
            catch
            {
                BeginAccept();
                return;
            }

            TcpClient _client = new TcpClient(_clientSocket);            
            
            Clients.Add(_client);

            _client.OnOpen(() =>
            {
                _OnEnter?.Invoke(this, _client);
            });

            _client.OnClose(() =>
            {
                foreach (TcpClient client in Clients.ToArray())
                {
                    if (_client.Id == ((TcpClient)client).Id)
                    {
                        Clients.Remove(client);
                    }
                }

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
                    _OnBeforeOpen?.Invoke(this, _socket);
                    
                    _socket.Bind(host.EndPoint);
                    _socket.Listen(0);

                    _host = host;
                    _opened = true;
                   
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
                    foreach (TcpClient client in Clients.ToArray())
                    {
                        try
                        {
                            client?.Close();
                        }
                        catch
                        {

                        }
                    }

                    Clients.Clear();
                    
                    _opened = false;
                    _tryClose = false;
                    _OnClose?.Invoke(this, null);
                }
            });
        }

        #endregion

        #region Client

        public void OnEnter(Action<TcpClient> callback)
        {
            _OnEnter += (_, client) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        public void OnExit(Action<TcpClient> callback)
        {
            _OnExit += (_, client) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        public void OnData(Action<TcpClient, byte[]> callback)
        {
            _OnData += (_, container) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(container.client, container.data);
                });
            };
        }

        public void OnEvent(Action<TcpClient, string, byte[]> callback)
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

        public void BeforeOpen(Action<Socket> callback)
        {
            _OnBeforeOpen += (_, socket) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        public void AfterOpen(Action<Socket> callback)
        {
            _OnAfterOpen += (_, socket) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        #endregion        
    }
}
