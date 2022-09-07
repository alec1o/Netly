using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Netly.Udp
{
    /// <summary>
    /// Udp: Server
    /// </summary>
    public class UdpServer : IUdpServer
    {
        #region Var

        #region Public

        /// <summary>
        /// Endpoint
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        /// TODO:
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Returns true if socket is connected
        /// </summary>
        public bool Opened { get => IsOpened(); }

        /// <summary>
        /// Returns list of UdpClient
        /// </summary>
        public List<UdpClient> Clients { get; private set; }

        #endregion

        #region Private

        private Socket _socket;

        private bool _tryOpen;
        private bool _tryClose;
        private bool _invokeClose;
        private bool _opened;

        #region Events

        private EventHandler _OnOpen;
        private EventHandler _OnClose;
        private EventHandler<Exception> _OnError;
        private EventHandler<UdpClient> _OnEnter;
        private EventHandler<UdpClient> _OnExit;
        private EventHandler<(UdpClient client, byte[] data)> _OnData;
        private EventHandler<(UdpClient client, string name, byte[] data)> _OnEvent;

        private EventHandler<Socket> _OnBeforeOpen;
        private EventHandler<Socket> _OnAfterOpen;

        #endregion

        #endregion

        #endregion

        #region Builder

        /// <summary>
        /// Creating instance
        /// </summary>
        public UdpServer()
        {
            Host = new Host(IPAddress.Any, 0);
            Clients = new List<UdpClient>();
            _socket = new Socket(Host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        #endregion

        #region Init

        /// <summary>
        /// Use to open connection
        /// </summary>
        /// <param name="host">Endpoint</param>
        /// <param name="backlog">Backlog</param>
        public void Open(Host host, int backlog = 0)
        {
            if (Opened || _tryOpen || _tryClose) return;

            _tryOpen = true;

            Async.SafePool(() =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _OnBeforeOpen?.Invoke(this, _socket);

                    _socket.Bind(host.EndPoint);

                    Host = host;

                    _opened = true;
                    _invokeClose = false;

                    _OnAfterOpen?.Invoke(this, _socket);

                    _OnOpen?.Invoke(this, EventArgs.Empty);

                    BeginAccept();
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

                    foreach (UdpClient client in Clients.ToArray())
                    {
                        try
                        {
                            client?.Close();
                        }
                        catch { }
                    }

                    _opened = false;
                    Clients.Clear();

                    if (!_invokeClose)
                    {
                        _invokeClose = true;
                        _OnClose?.Invoke(this, EventArgs.Empty);
                    }
                }

                _tryClose = false;
            });
        }

        private bool IsOpened()
        {
            if (_socket == null) return false;

            return _opened;
        }

        private void BeginAccept()
        {
            try
            {
                byte[] buffer = new byte[1024 * 8];
                int length = 0;
                EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

                while (!_invokeClose)
                {
                    try
                    {
                        length = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);

                        byte[] data = new byte[length];

                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);

                        IPEndPoint ipe = (IPEndPoint)endpoint;

                        EndAccept(ipe.Address , ipe.Port, data);                        
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
            }
            catch
            {
                if (Opened)
                {
                    BeginAccept();
                }
            }
        }

        private void EndAccept(IPAddress address, int port, byte[] data)
        {
            // find udp client
            foreach (UdpClient target in Clients.ToArray())
            {
                if (target.Host.Port == port && target.Host.Address.ToString() == address.ToString())
                {
                    target.AddData(data);

                    if (data.Length <= 0)
                    {
                        target.Close();
                        Clients.Remove(target);
                        _OnExit?.Invoke(this, target);
                    }
                    
                    return;        
                }
            }

            // create new client

            UdpClient client = new UdpClient(Guid.NewGuid().ToString(), ref _socket, new Host(address, port));
            Clients.Add(client);

            client.OnOpen(() =>
            {
                _OnEnter?.Invoke(this, client);
            });

            client.OnClose(() =>
            {
                foreach (UdpClient target in Clients.ToArray())
                {
                    if (client.Id == target.Id)
                    {
                        try
                        {
                            Clients.Remove(target);
                        }
                        catch { }

                        _OnExit?.Invoke(this, client);
                    }
                }
            });

            client.OnData((value) =>
            {
                _OnData?.Invoke(this, (client, value));
            });

            client.OnEvent((name, value) =>
            {
                _OnEvent?.Invoke(this, (client, name, value));
            });

            client.InitServer();
        }

        /// <summary>
        /// TODO:
        /// </summary>
        /// <param name="value">TODO:</param>
        public void UseTimeout(int value)
        {
            if (Opened)
            {
                throw new Exception("Error, you can't add timeout configuration to an open socket");
            }

            Timeout = Math.Abs(value);
        }

        /// <summary>
        /// Sends raw data to all connected clients
        /// </summary>
        /// <param name="data">The date to be published</param>
        public void BroadcastToData(byte[] data)
        {
            if (!Opened || data == null) return;

            foreach (UdpClient client in Clients.ToArray())
            {
                client.ToData(data);
            }
        }

        /// <summary>
        /// Sends formatted "event" data to all connected clients
        /// </summary>
        /// <param name="name">Event name "subscription"</param>
        /// <param name="data">The date to be published</param>
        public void BroadcastToEvent(string name, byte[] data)
        {
            if (!Opened || data == null) return;

            foreach (UdpClient client in Clients.ToArray())
            {
                client.ToEvent(name, data);
            }
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

        #region Events

        /// <summary>
        ///  Execute the callback, when: the connection is opened
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
        /// Execute the callback, when: a client connects
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnEnter(Action<UdpClient> callback)
        {
            _OnEnter += (sender, client) =>
            {
                Call.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: a client disconnects
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnExit(Action<UdpClient> callback)
        {
            _OnExit += (sender, client) =>
            {
                Call.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: the connection receives raw data
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnData(Action<UdpClient, byte[]> callback)
        {
            _OnData += (sender, value) =>
            {
                Call.Execute(() =>
                {
                    callback?.Invoke(value.client, value.data);
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: the connection receives event data
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnEvent(Action<UdpClient, string, byte[]> callback)
        {
            _OnEvent += (sender, value) =>
            {
                Call.Execute(() =>
                {
                    callback?.Invoke(value.client, value.name, value.data);
                });
            };
        }

        #endregion
    }
}