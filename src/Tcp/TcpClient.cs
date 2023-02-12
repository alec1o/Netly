using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Byter;
using Netly.Core;

namespace Netly
{
    /// <summary>
    /// Netly: TcpClient
    /// </summary>
    public class TcpClient : IClient
    {
        #region Var        

        /// <summary>
        /// Host
        /// </summary>
        public Host Host { get; private set; }


        /// <summary>
        /// Returns true if socket is connected
        /// </summary>
        public bool IsOpened { get => Connected(); }

        internal string Id;

        private Socket _socket;

        private bool _isServer;
        private bool _tryOpen;
        private bool _tryClose;
        private bool _invokeClose;

        private EventHandler _OnOpen;
        private EventHandler _OnClose;
        private EventHandler<Exception> _OnError;
        private EventHandler<byte[]> _OnData;
        private EventHandler<(string name, byte[] data)> _OnEvent;
        private EventHandler<Socket> _OnModify;

        #endregion


        #region Builder

        /// <summary>
        /// Netly: TcpClient
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
            _socket.NoDelay = true;
            Host = new Host(socket.RemoteEndPoint ?? new IPEndPoint(IPAddress.Any, 0));
        }

        internal void InitServer()
        {
            _OnOpen?.Invoke(null, null);
            Receive();
        }

        #endregion

        #region Customization Event

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


        #endregion

        #region Init

        /// <summary>
        /// Use to open connection
        /// </summary>
        /// <param name="host">Endpoint</param>
        public void Open(Host host)
        {
            if (IsOpened || _tryOpen || _tryClose || _isServer) return;

            _tryOpen = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    _socket.NoDelay = true;

                    _OnModify?.Invoke(null, _socket);

                    _socket.Connect(host.EndPoint);

                    Host = host;

                    _invokeClose = false;

                    _OnOpen?.Invoke(null, null);

                    Receive();
                }
                catch (Exception e)
                {
                    _OnError?.Invoke(null, e);
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

                    if (!_invokeClose)
                    {
                        _invokeClose = true;
                        _OnClose?.Invoke(null, null);
                    }
                }

                _tryClose = false;
            });
        }

        /// <summary>
        /// Use to send raw data
        /// </summary>
        /// <param name="value">raw data</param>
        public void ToData(byte[] value)
        {
            if (_invokeClose) return;

            List<byte[]> list = new List<byte[]>
            {
                BitConverter.GetBytes(value.Length),
                value
            };

            byte[] buffer = list.SelectMany(b => b).ToArray();

            _socket.Send(buffer, SocketFlags.None);
            Console.WriteLine($"\t<SEND {buffer.Length} Bytes>");
        }

        /// <summary>
        /// Use to send event (certain event) 
        /// </summary>
        /// <param name="name">event name</param>
        /// <param name="value">event data</param>
        public void ToEvent(string name, byte[] value)
        {
            ToData(MessageParser.Create(name, value));
        }

        /// <summary>
        /// Use to send raw data
        /// </summary>
        /// <param name="value">raw data</param>
        public void ToData(string value)
        {
            this.ToData(NE.GetBytes(value, NE.Mode.UTF8));
        }

        /// <summary>
        /// Use to send event (certain event)
        /// </summary>
        /// <param name="name">event name</param>
        /// <param name="value">event data</param>
        public void ToEvent(string name, string value)
        {
            this.ToEvent(name, NE.GetBytes(value, NE.Mode.UTF8));
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


            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!_invokeClose)
                {
                    try
                    {
                        length = _socket.Receive(buffer, SocketFlags.None);
                        Console.WriteLine($"\t<RECEIVE {length} Bytes>");

                        if (length <= 0)
                        {
                            if (IsOpened is false) break;
                            continue;
                        }

                        byte[] data = new byte[length];
                        Array.Copy(buffer, 0, data, 0, data.Length);

                        int m_size = BitConverter.ToInt32(data, 0);
                        if (m_size == data.Length - sizeof(Int32))
                        {
                            Deploy(data);
                        }
                        else
                        {
                            int _index = 0;
                            while (_index < data.Length)
                            {
                                int _size = BitConverter.ToInt32(data, _index);
                                _index += sizeof(Int32);

                                Console.WriteLine("TRY RECEIVE: " + _size);

                                byte[] _data = new byte[_size];
                                Array.Copy(data, _index + (sizeof(Int16)), _data, 0, _data.Length);
                                _index += _size;

                                Deploy(_data);
                            }
                            Console.WriteLine("FINISH RECEIVE");
                        }


                    }
                    catch
                    {
                        if (length <= 0)
                        {
                            if (IsOpened is false) break;
                            continue;
                        }
                    }
                }

                if (!_tryClose || !_invokeClose)
                {
                    _invokeClose = true;
                    _OnClose?.Invoke(null, null);
                }
            });

        }

        private void Deploy(byte[] data)
        {
            (string name, byte[] data) result = MessageParser.Verify(data);

            if (result.data == null)
                _OnData?.Invoke(null, data);
            else
                _OnEvent?.Invoke(null, (result.name, result.data));
        }

        #endregion

        #region Events

        /// <summary>
        /// Execute the callback, when: the connection is opened
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
        /// Execute the callback, when: when the connection receives raw data
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnData(Action<byte[]> callback)
        {
            _OnData += (sender, data) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(data);
                });
            };
        }

        /// <summary>
        /// Execute the callback, when: when the connection receives event data
        /// </summary>
        /// <param name="callback">action/callback</param>
        public void OnEvent(Action<string, byte[]> callback)
        {
            _OnEvent += (sender, result) =>
            {
                MainThread.Add(() =>
                {
                    callback?.Invoke(result.name, result.data);
                });
            };
        }

        #endregion
    }
}
