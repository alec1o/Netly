using System;
using System.Net;
using Zenet.Network;
using System.Net.Sockets;
using Zenet.Package;
using System.Linq;

namespace Zenet.Udp
{
    public class ClientUDP
    {
        #region VAR

        public Host Host { get; private set; }
        private readonly Socket SocketMirror;
        public Socket Socket { get; private set; }
        private EventHandler<object> OnOpenEvent, OnErrorEvent, OnCloseEvent, OnReceiveEvent;
        private bool closed, tryOpen, tryClose, opened;
        public bool Opened => opened;

        #endregion


        #region INIT

        public ClientUDP(Socket socket = null)
        {
            SocketMirror = socket ?? new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        #endregion


        #region OPEN


        public void Open(Host host)
        {
            if (tryOpen || Opened) return;
            tryOpen = true;

            Host = host;

            Async.Thread(() =>
            {
                Socket?.Dispose();
                Socket = EasySocket.CopySocket(SocketMirror);

                try
                {
                    Socket.Connect(Host.EndPoint);
                    closed = false;
                    opened = true;
                    Receive();
                    OnOpenEvent?.Invoke(this, null);
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
                var host = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
                while (Opened)
                {
                    try
                    {
                        var size = Socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref host);

                        if (size < 1)
                        {
                            Close();
                            continue;
                        }

                        var data = new byte[size];
                        Buffer.BlockCopy(buffer, 0, data, 0, size);

                        if (data.Length == ServerUDP.CloseEventData.Length && Compare.Bytes(data, ServerUDP.CloseEventData))
                        {
                            Close();
                            continue;
                        }

                        OnReceiveEvent?.Invoke(this, data);
                    }
                    catch {  }
                }
            });
        }

        #endregion


        #region CLOSE

        public void Close()
        {
            if (tryClose || closed || !opened) return;
            tryClose = true;

            closed = true;
            opened = false;
            tryClose = false;

            // send 10x close message to client
            for (int i = 0; i < 10; i++)
            {
                Send(ServerUDP.CloseEventData, false);
            }


            Async.Thread(() =>
            {          
                if (Opened)
                {                   
                    Socket?.Close();
                }

                Socket?.Shutdown(SocketShutdown.Both);
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
            if (data == null || data.Length < 1 && opened) return;
            try
            {
                if (async)
                {
                    Socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, Host.EndPoint, null, null);
                    return;
                }

                Socket.SendTo(data, 0, data.Length, SocketFlags.None, Host.EndPoint);
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
