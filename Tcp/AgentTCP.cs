using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Zenet.Package;
using Zenet.Network;


namespace Zenet.Tcp
{
    [Obsolete("This version is obsolete. Use TcpClient or TcpServer")]
    public class AgentTCP
    {
        #region VAR

        public readonly IPEndPoint Host;
        public readonly Socket Socket;
        private EventHandler<object> OnOpenEvent, OnCloseEvent, OnReceiveEvent;
        private bool closed, tryClose;
        private readonly NetworkStream stream;
        public bool Opened => Connected();
        public readonly string Id;

        #endregion


        #region INIT

        public AgentTCP(Socket socket)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            stream = new NetworkStream(Socket, true);
            Host = Socket.RemoteEndPoint as IPEndPoint;
            Id = Guid.NewGuid().ToString();
            Init();
        }

        #endregion


        #region OPEN

        private bool Connected()
        {
            var result = IsRunning();

            if (!result && !closed)
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

        private void Init()
        {
            Async.Thread(() =>
            {
                Async.Thread(() =>
                {
                    Thread.Sleep(10);
                    OnOpenEvent?.Invoke(this, null);
                });

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
            if (tryClose || closed) return;
            tryClose = true;

            Async.Thread(() =>
            {
                closed = true;

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

        public void OnReceive(Action<byte[]> callback)
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
