using System;
using System.Net.Sockets;
using Netly.Core;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    /// <summary>
    /// Netly, Udp client implementation
    /// </summary>
    public class UdpClient : IUdpClient
    {
        internal const int _DEFAULT_TIMEOUT_VALUE = 5000;
        internal const int _MIN_TIMEOUT_VALUE = 1000;
        private const byte _PING_PER_SECOND = 8;
        private const byte _CONNECTION_PREFIX = 1;

        /// <summary>
        /// Return true when connection is opened
        /// </summary>
        public bool IsOpened => _socket != null;

        /// <summary>
        /// Host container
        /// </summary>
        public Host Host { get; private set; } = Host.Default;

        /// <summary>
        /// Return true when connection is enabled (using ping)
        /// </summary>
        public bool UseConnection { get; private set; }

        /// <summary>
        /// Return timeout value, is milliseconds
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Unique User Identifier, only server-side client
        /// </summary>
        public readonly string UUID = "Powered by @alec1o";

        private EventHandler _onOpen;
        private EventHandler _onClose;
        private EventHandler<Exception> _onError;
        private EventHandler<byte[]> _onData;
        private EventHandler<(string name, byte[] buffer)> _onEvent;
        private EventHandler<Socket> _onModify;

        internal Action<byte[]> AddBuffer;

        private Socket _socket;
        private bool _opened;
        private readonly bool _serverside;
        private readonly object _lock = new object();


        /// <summary>
        /// Create udp client instance
        /// </summary>
        /// <param name="useConnection">enable or disable connection (using ping)</param>
        /// <param name="timeout">connection timeout, is milliseconds and only work if useConnection is enabled<br/>min: 1000<br/>default: 5000</param>
        public UdpClient(bool useConnection, int timeout = _DEFAULT_TIMEOUT_VALUE)
        {
            Timeout = timeout;
            UseConnection = useConnection;
            _opened = false;
            _serverside = false;
            _socket = null;
            Init();
        }

        internal UdpClient(string uuid, ref Socket socket, Host host, bool useConnection, int timeout)
        {
            _serverside = true;
            _socket = socket;
            _opened = true;
            Host = host;
            UUID = uuid;
            UseConnection = useConnection;
            Timeout = timeout;
            Receive();
        }

        # region Worker

        private void Init()
        {
            if (UseConnection)
            {
                if (Timeout < _MIN_TIMEOUT_VALUE)
                {
                    string message =
                        $"{nameof(Timeout)} min value required for timeout is {_MIN_TIMEOUT_VALUE}, " +
                        $"current value is: {Timeout}, default value is: {_DEFAULT_TIMEOUT_VALUE}";
                    throw new Exception(message);
                }
            }
        }

        private void Receive()
        {
            // it is timeout timer/counter
            DateTime connectionTimer = new DateTime();

            // it will refresh connectionTimer when called/invoked
            void Refresh()
            {
                // Reset , it always will add milliseconds inside current time
                connectionTimer = DateTime.Now.AddMilliseconds(Timeout);
            }

            // it will called for publish buffer using (OnData or OnEvent channel)
            void Publish(ref byte[] buffer)
            {
                // update connection
                if (UseConnection)
                {
                    // it will refresh connectionTimer (timeout timer/counter)
                    Refresh();
                }

                // Ignore message if size is 1 and content/value = _CONNECTION_PREFIX because is ping message
                // And prevent publish PING data when UseConnection is disabled (false)
                if (buffer.Length == 1 && buffer[0] == _CONNECTION_PREFIX)
                {
                    return;
                }

                // verify is this buffer is a event (netly-event)
                (string name, byte[] buffer) content = EventManager.Verify(buffer);

                // publish buffer
                if (content.buffer == null || content.name == null)
                {
                    // always when buffer or name is null, it isn't event (netly-event)
                    _onData?.Invoke(null, buffer);
                }
                else
                {
                    // always that buffer or name contain value is netly event
                    _onEvent?.Invoke(null, (content.name, content.buffer));
                }
            }

            if (_serverside)
            {
                // receive data on server-side
                AddBuffer = (data) =>
                {
                    Publish(ref data);
                };
            }
            else
            {
                // receive data on client-side
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    byte[] buffer = new byte[MessageFraming.UdpBuffer];
                    EndPoint server = new IPEndPoint(IPAddress.Any, 0);

                    while (IsOpened)
                    {
                        try
                        {
                            int length = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref server);

                            if (length <= 0)
                            {
                                break;
                            }

                            byte[] data = new byte[length];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            Publish(ref data);
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    Close();
                });
            }

            if (UseConnection)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    // init connectionTimer
                    Refresh();

                    while (connectionTimer > DateTime.Now && IsOpened)
                    {
                        ToData(new[] { _CONNECTION_PREFIX });
                        Thread.Sleep(1000 / _PING_PER_SECOND);
                    }

                    Close();
                });
            }
        }

        #endregion

        #region Triggers

        /// <summary>
        /// Open udp connection
        /// </summary>
        /// <param name="host">Endpoint container</param>
        public void Open(Host host)
        {
            Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_opened || _serverside)
                    {
                        return;
                    }

                    try
                    {
                        _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                        _onModify?.Invoke(null, _socket);

                        _socket.Connect(host.EndPoint);

                        Host = new Host(_socket.RemoteEndPoint);

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
        /// Close udp connection
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

                        if (_serverside)
                        {
                            if (_opened)
                            {
                                _opened = false;
                                _onClose?.Invoke(null, null);
                            }
                        }
                        else
                        {
                            try
                            {
                                _socket?.Close();
                                _socket?.Dispose();
                            }
                            finally
                            {
                                _socket = null;
                                _opened = false;
                                _onClose?.Invoke(null, null);
                            }
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Send raw buffer
        /// </summary>
        /// <param name="buffer">buffer</param>
        public void ToData(byte[] buffer)
        {
            try
            {
                if (IsOpened && buffer != null && buffer.Length > 0)
                {
                    _socket?.SendTo(buffer, SocketFlags.None, Host.EndPoint);
                }
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Send raw buffer
        /// </summary>
        /// <param name="buffer">buffer</param>
        public void ToData(string buffer)
        {
            if (!string.IsNullOrEmpty(buffer))
            {
                ToData(NE.GetBytes(buffer));
            }
        }

        /// <summary>
        /// Send event (netly-event)<br/><br/>
        /// You can create event using:<br/>
        /// <code>
        /// Netly.Core.EventManager.Create(string name, byte[] data);<br/>
        /// </code>
        /// <br/>And you can verify is a buffer is event (netly-event) using: <br/>
        /// <code>
        /// // When name or data is null, it mean isn't event (netly-event) 
        /// (string name, byte[] data) = Netly.Core.EventManager.Verify(byte[] data);
        /// </code>
        /// </summary>
        /// <param name="name">name of event</param>
        /// <param name="buffer">buffer of event</param>
        public void ToEvent(string name, byte[] buffer)
        {
            if (!string.IsNullOrEmpty(name) && buffer != null && buffer.Length > 0)
            {
                ToData(EventManager.Create(name, buffer));
            }
        }

        /// <summary>
        /// Send event (netly-event)<br/><br/>
        /// You can create event using:<br/>
        /// <code>
        /// Netly.Core.EventManager.Create(string name, byte[] data);<br/>
        /// </code>
        /// <br/>And you can verify is a buffer is event (netly-event) using: <br/>
        /// <code>
        /// // When name or data is null, it mean isn't event (netly-event) 
        /// (string name, byte[] data) = Netly.Core.EventManager.Verify(byte[] data);
        /// </code>
        /// </summary>
        /// <param name="name">name of event</param>
        /// <param name="buffer">buffer of event</param>
        public void ToEvent(string name, string buffer)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(buffer))
            {
                ToEvent(name, NE.GetBytes(buffer));
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Callback will called when is connection opened
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
        /// Callback will called when connection isn't opened
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
        /// Callback will called when connection is closed
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
        /// Callback will called when receive raw data
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnData(Action<byte[]> callback)
        {
            _onData += (_, buffer) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(buffer);
                });
            };
        }

        /// <summary>
        /// Callback will called when receive event (netly-event)<br/><br/>
        /// You can create event using:<br/>
        /// <code>
        /// Netly.Core.EventManager.Create(string name, byte[] data);<br/>
        /// </code>
        /// <br/>And you can verify is a buffer is event (netly-event) using: <br/>
        /// <code>
        /// // When name or data is null, it mean isn't event (netly-event) 
        /// (string name, byte[] data) = Netly.Core.EventManager.Verify(byte[] data);
        /// </code>
        /// </summary>
        /// <param name="callback">callback</param>
        public void OnEvent(Action<string, byte[]> callback)
        {
            _onEvent += (_, data) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(data.name, data.buffer);
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

        #endregion
    }
}
