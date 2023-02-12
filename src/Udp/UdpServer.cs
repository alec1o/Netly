using Netly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Netly
{
    /// <summary>
    /// Netly: UdpServer
    /// </summary>
    public class UdpServer : IUdpServer
    {
        #region Var

        /// <summary>
        /// Endpoint
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        /// Returns true if socket is connected
        /// </summary>
        public bool IsOpened { get => Connected(); }

        /// <summary>
        /// Returns list of UdpClient
        /// </summary>
        public List<UdpClient> Clients { get; private set; }

        private Socket _socket;

        private bool _tryOpen;
        private bool _tryClose;
        private bool _invokeClose;
        private bool _opened;

        private EventHandler _OnOpen;
        private EventHandler _OnClose;
        private EventHandler<Exception> _OnError;
        private EventHandler<UdpClient> _OnEnter;
        private EventHandler<UdpClient> _OnExit;
        private EventHandler<(UdpClient client, byte[] data)> _OnData;
        private EventHandler<(UdpClient client, string name, byte[] data)> _OnEvent;

        private EventHandler<Socket> _OnModify;

        #endregion


        /// <summary>
        /// Creating instance
        /// </summary>
        public UdpServer()
        {
            Host = new Host(IPAddress.Any, 0);
            Clients = new List<UdpClient>();
            _socket = new Socket(Host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        #region Init

        /// <summary>
        ///  Use to open connection
        /// </summary>
        /// <param name="host"></param>
        public void Open(Host host) => Open(host, 0);

        /// <summary>
        /// Use to open connection
        /// </summary>
        /// <param name="host">Endpoint</param>
        /// <param name="backlog">Backlog</param>
        public void Open(Host host, int backlog = 0)
        {
            if (IsOpened || _tryOpen || _tryClose) return;

            _tryOpen = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    _OnModify?.Invoke(this, _socket);

                    _socket.Bind(host.EndPoint);

                    Host = host;

                    _opened = true;
                    _invokeClose = false;

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
            if (!IsOpened || _tryOpen || _tryClose) return;

            _tryClose = true;

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

        private bool Connected()
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

                        EndAccept(ipe.Address, ipe.Port, data);
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
            }
            catch
            {
                if (IsOpened)
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
                    // add received data
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

            // add this instance in list of all clients connected
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

            // init this instance for start work
            client.InitServer();

            // add received data
            client.AddData(data);
        }

        /// <summary>
        /// Sends raw data to all connected clients
        /// </summary>
        /// <param name="data">The date to be published</param>
        public void BroadcastToData(byte[] data)
        {
            if (!IsOpened || data == null) return;

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
            if (!IsOpened || data == null) return;

            foreach (UdpClient client in Clients.ToArray())
            {
                client.ToEvent(name, data);
            }
        }

        #endregion

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

        #region Events

        /// <summary>
        ///  Execute the callback, when: the connection is opened
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
        /// Execute the callback, when: a client connects
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnEnter(Action<UdpClient> callback)
        {
            _OnEnter += (sender, client) =>
            {
                MainThread.Add(() =>
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
                MainThread.Add(() =>
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
                MainThread.Add(() =>
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
                MainThread.Add(() =>
                {
                    callback?.Invoke(value.client, value.name, value.data);
                });
            };
        }

        #endregion
    }
}