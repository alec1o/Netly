using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    /// Netly, Udp server implementation
    /// </summary>
    public class UdpServer : IUdpServer
    {
        /// <summary>
        /// Host container
        /// </summary>
        public Host Host { get; private set; }

        /// <summary>
        /// Connection timeout
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Return true when udp server is bind
        /// </summary>
        public bool IsOpened => _socket != null;

        /// <summary>
        /// Return true when udp connection is enabled
        /// </summary>
        public bool UseConnection { get; private set; }

        /// <summary>
        /// Return array of connected client
        /// </summary>
        public UdpClient[] Clients => GetAllClients();

        private EventHandler _onOpen;
        private EventHandler _onClose;
        private EventHandler<Exception> _onError;
        private EventHandler<(UdpClient client, byte[] buffer)> _onData;
        private EventHandler<(UdpClient client, string name, byte[] buffer)> _onEvent;
        private EventHandler<Socket> _onModify;
        private EventHandler<UdpClient> _onEnter;
        private EventHandler<UdpClient> _onExit;

        private Socket _socket;
        private bool _opened;
        private readonly object _lock = new object();
        private readonly List<UdpClient> _clients = new List<UdpClient>();

        /// <summary>
        /// Create udp client instance
        /// </summary>
        /// <param name="useConnection">enable or disable connection (using ping)</param>
        /// <param name="timeout">connection timeout, is milliseconds and only work if useConnection is enabled<br/>min: 1000<br/>default: 5000</param>
        public UdpServer(bool useConnection, int timeout = UdpClient._DEFAULT_TIMEOUT_VALUE)
        {
            Timeout = timeout;
            UseConnection = useConnection;
            Init();
        }

        # region Worker

        private void Init()
        {
            if (UseConnection)
            {
                if (Timeout < UdpClient._MIN_TIMEOUT_VALUE)
                {
                    string message =
                        $"{nameof(Timeout)} min value required for timeout is {UdpClient._MIN_TIMEOUT_VALUE}, " +
                        $"current value is: {Timeout}, default value is: {UdpClient._DEFAULT_TIMEOUT_VALUE}";
                    throw new Exception(message);
                }
            }
        }

        private UdpClient[] GetAllClients()
        {
            UdpClient[] clients;

            lock (_lock)
            {
                clients = _clients.ToArray();
            }

            return clients;
        }

        void AddClient(UdpClient client)
        {
            lock (_lock)
            {
                _clients.Add(client);
            }
        }

        void RemoveClient(UdpClient client)
        {
            lock (_lock)
            {
                _clients.Remove(client);
            }
        }

        private void Receive()
        {
            // Init trading
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // Inside threader
                byte[] buffer = new byte[MessageFraming.UdpBuffer];
                EndPoint client = new IPEndPoint(IPAddress.Any, 0);

                while (IsOpened)
                {
                    try
                    {
                        int length = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref client);

                        if (length <= 0)
                        {
                            if (IsOpened)
                            {
                                continue;
                            }

                            break;
                        }

                        byte[] data = new byte[length];

                        Array.Copy(buffer, 0, data, 0, data.Length);

                        EndReceive(ref client, data);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                Close();
            });

            void EndReceive(ref EndPoint endpoint, byte[] buffer)
            {
                // e.g: "127.0.0.1:10101"
                string rawHost = ((IPEndPoint)endpoint).ToString();

                // find user
                foreach (var client in Clients)
                {
                    if (client.Host.ToString() == rawHost)
                    {
                        client.AddBuffer(buffer);
                        return;
                    }
                }

                UdpClient myClient = new UdpClient
                (
                    uuid: Guid.NewGuid().ToString(),
                    socket: ref _socket,
                    host: new Host(endpoint),
                    useConnection: UseConnection,
                    timeout: Timeout
                );

                myClient.OnClose(() =>
                {
                    RemoveClient(myClient);
                    _onExit?.Invoke(null, myClient);
                });

                myClient.OnData((data) =>
                {
                    _onData?.Invoke(null, (myClient, data));
                });

                myClient.OnEvent((name, data) =>
                {
                    _onEvent?.Invoke(null, (myClient, name, data));
                });


                AddClient(myClient); // Add client on list of clients

                _onEnter?.Invoke(null, myClient); // Init client on server-side

                myClient.AddBuffer(buffer); // Invoke OnData on client-side
            }
        }

        #endregion

        #region Triggers

        /// <summary>
        /// Start udp server (bind client)
        /// </summary>
        /// <param name="host">Endpoint container</param>
        public void Open(Host host)
        {
            Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_opened)
                    {
                        return;
                    }

                    try
                    {
                        _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                        _onModify?.Invoke(null, _socket);

                        _socket.Bind(host.IPEndPoint);
                        
                        // Host must be local because server bind in local endpoint
                        Host = new Host(_socket.LocalEndPoint);

                        _opened = true;

                        _onOpen?.Invoke(this, null);

                        Receive();
                    }
                    catch (Exception e)
                    {
                        _onError?.Invoke(null, e);
                    }
                }
            });
        }

        /// <summary>
        /// Close udp connection and disconnect all connected client
        /// </summary>
        public void Close()
        {
            Task.Run(() =>
            {
                Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (!_opened)
                        {
                            return;
                        }

                        try
                        {
                            foreach (var client in _clients)
                            {
                                try
                                {
                                    client?.Close();
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }

                            _socket?.Close();
                            _socket?.Dispose();
                        }
                        finally
                        {
                            _socket = null;
                            _opened = false;
                            _clients.Clear();
                            _onClose?.Invoke(null, null);
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Broadcast raw data for all connected client
        /// </summary>
        /// <param name="buffer">buffer (raw data)</param>
        public void ToData(byte[] buffer)
        {
            foreach (var client in Clients)
            {
                if (client != null)
                {
                    client.ToData(buffer);
                }
            }
        }

        /// <summary>
        /// Broadcast raw data for all connected client
        /// </summary>
        /// <param name="buffer">buffer (raw data)</param>
        public void ToData(string buffer)
        {
            if (!string.IsNullOrEmpty(buffer))
            {
                ToData(NE.GetBytes(buffer));
            }
        }

        /// <summary>
        /// Broadcast event (netly-event) for all connected client
        /// </summary>
        /// <param name="name">event name</param>
        /// <param name="buffer">event buffer</param>
        public void ToEvent(string name, byte[] buffer)
        {
            if (!string.IsNullOrEmpty(name) && buffer != null && buffer.Length > 0)
            {
                ToData(EventManager.Create(name, buffer));
            }
        }

        /// <summary>
        /// Broadcast event (netly-event) for all connected client
        /// </summary>
        /// <param name="name">event name</param>
        /// <param name="buffer">event buffer</param>
        public void ToEvent(string name, string buffer)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(buffer))
            {
                ToData(EventManager.Create(name, NE.GetBytes(buffer)));
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Callback will called when is connection opened (server start bind)
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnOpen(Action callback)
        {
            _onOpen += (_, __) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        /// <summary>
        /// Callback will called when connection isn't opened (error on open connection)
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnError(Action<Exception> callback)
        {
            _onError += (_, exception) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(exception);
                });
            };
        }

        /// <summary>
        /// Callback will called when server closed connection
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnClose(Action callback)
        {
            _onClose += (_, __) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        /// <summary>
        /// Callback will called when a client receive raw data
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnData(Action<UdpClient, byte[]> callback)
        {
            _onData += (_, data) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(data.client, data.buffer);
                });
            };
        }

        /// <summary>
        /// Callback will called when a client receive event (netly-event)
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnEvent(Action<UdpClient, string, byte[]> callback)
        {
            _onEvent += (_, data) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(data.client, data.name, data.buffer);
                });
            };
        }

        /// <summary>
        /// Callback will called after create Socket Instance, and before open connection<br/>
        /// You can use the socket for modify default values
        /// </summary>
        /// <param name="callback"></param>
        public void OnModify(Action<Socket> callback)
        {
            _onModify += (_, socket) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(socket);
                });
            };
        }

        /// <summary>
        /// / Callback will called when a client connect
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnEnter(Action<UdpClient> callback)
        {
            _onEnter += (_, client) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        /// <summary>
        /// Callback will called when a client disconnected from server
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnExit(Action<UdpClient> callback)
        {
            _onExit += (_, client) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        #endregion
    }
}
