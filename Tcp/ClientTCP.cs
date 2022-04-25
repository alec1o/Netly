using System;
using System.Net.Sockets;
using Zenet.Package;
using Zenet.Network;

namespace Zenet.Tcp
{
    public class ClientTCP
    {
        #region VAR

        public Host Host { get; private set; }
        private readonly Socket SocketMirror;
        public Socket Socket { get; private set; }
        private EventHandler<object> OnOpenEvent, OnErrorEvent, OnCloseEvent, OnReceiveEvent;
        private bool closed, tryOpen, tryClose, oneConnection;
        private NetworkStream stream;
        public bool Opened => Connected();

        #endregion


        #region INIT

        public ClientTCP(Socket socket = null)
        {
            SocketMirror = socket ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        #endregion


        #region OPEN

        private bool Connected()
        {
            var result = IsRunning();

            if (oneConnection && !result && !closed)
            {
                Close();
            }

            return result;

            bool IsRunning()
            {
                if (Socket == null || stream == null || !Socket.Connected) return false;

                try
                {
                    return !(Socket.Poll(5000, SelectMode.SelectRead) && Socket.Available == 0);
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Open(Host host)
        {
            if (tryOpen || Opened) return;
            tryOpen = true;

            Host = host;

            Async.Thread(() =>
            {
                Socket = EasySocket.CopySocket(SocketMirror);

                try
                {
                    Socket.Connect(Host.EndPoint);
                    stream = new NetworkStream(Socket, true);
                    OnOpenEvent?.Invoke(this, null);
                    closed = false;
                    oneConnection = true;
                    Receive();
                }
                catch (Exception e)
                {
                    OnErrorEvent?.Invoke(this, e);
                }

                tryOpen = false;
            });
        }

        private void Receive()
        {
            Async.Thread(() =>
            {
                var buffer = new byte[1024 * 8];

                while (Opened)
                {
                    try
                    {
                        var size = stream.Read(buffer, 0, buffer.Length);

                        if (size < 1) continue;

                        var data = new byte[size];
                        Buffer.BlockCopy(buffer, 0, data, 0, size);
                        OnReceiveEvent?.Invoke(this, data);
                    }
                    catch { }
                }
            });
        }

        #endregion


        #region CLOSE

        public void Close()
        {
            if (!oneConnection || tryClose || closed) return;
            tryClose = true;

            Async.Thread(() =>
            {
                closed = true;
                tryClose = false;

                if (Opened)
                {
                    stream?.Close();
                    Socket?.Close();
                }

                OnCloseEvent?.Invoke(this, null);
            });
        }

        #endregion


        #region SEND

        public void Send(string message, Encode encode = Encode.UTF8, bool async = true)
        {
            Send(Encoding2.Bytes(message, encode), async);
        }

        public void Send(byte[] data, bool async = true)
        {
            if (data == null || data.Length < 1) return;

            try
            {
                if (async)
                {
                    stream.BeginWrite(data, 0, data.Length, null, null);
                    return;
                }

                stream.Write(data, 0, data.Length);
            }
            catch
            {
                if (!Opened) return;

                Send(data, async);
            }
        }

        #endregion


        #region EVENT

        public void OnOpen(Action callback)
        {
            OnOpenEvent += (_, __) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        public void OnError(Action<Exception> callback)
        {
            OnErrorEvent += (_, e) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke((Exception)e);
                });
            };
        }

        public void OnClose(Action callback)
        {
            OnCloseEvent += (_, __) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke();
                });
            };
        }

        public void OnData(Action<byte[]> callback)
        {
            OnReceiveEvent += (_, data) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke((byte[])data);
                });
            };
        }

        #endregion
    }
}