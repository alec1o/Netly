using System;
using System.Threading;
using System.Net.Sockets;
using Zenet.Network;
using Zenet.Package;

namespace Zenet.Udp
{
    public class AgentUDP
    {
        public readonly ServerUDP Server;
        public bool Opened { get; private set; }
        public Host Host { get; private set; }
        public readonly string Id;
        private int timeout;

        private EventHandler OnOpenEvent;
        private EventHandler OnCloseEvent;
        private EventHandler<byte[]> OnDataEvent;
        private bool updated, one;

        public AgentUDP(ServerUDP Server, Host host, int timeout)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Server = Server;
            this.Host = host;
            this.timeout = timeout;            
        }

        internal static void SetData(byte[] data, AgentUDP agent)
        {
            agent.SetData(data);
        }

        private void SetData(byte[] data)
        {
            if (!one)
            {
                one = true;
                Opened = true;
                OnOpenEvent?.Invoke(this, null);
                if (timeout > 0) InitTimeout();
            }

            if (Opened)
            {
                updated = true;
                OnDataEvent?.Invoke(this, data);
            }
        }
        
        private void InitTimeout()
        {
            Async.Thread(() =>
            {
                while (Opened)
                {
                    updated = false;

                    Thread.Sleep(timeout);

                    if (!updated)
                    {
                        Close();
                    }
                }
            });
        }        

        public void Close()
        {
            if (Opened)
            {
                // send 10x close message to client
                for (int i = 0; i < 10; i++)
                {
                    Send(ServerUDP.CloseEventData, false);
                }

                Opened = false;
                OnCloseEvent?.Invoke(this, null);
            }
        }

        public void OnOpen(Action callback)
        {
            OnOpenEvent += ((_, __) =>
            {
                Callback.Execute(() => callback?.Invoke());
            });
        }

        public void OnClose(Action callback)
        {
            OnCloseEvent += ((_, __) =>
            {
                Callback.Execute(() => callback?.Invoke());
            });
        }
        
        public void OnData(Action<byte[]> callback)
        {
            OnDataEvent += ((_, data) =>
            {
                Callback.Execute(() => callback?.Invoke(data));
            });
        }

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
                    Server.Socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, Host.EndPoint, null, null);
                    return;
                }

                Server.Socket.SendTo(data, 0, data.Length, SocketFlags.None, Host.EndPoint);
            }
            catch
            {
                if (!Opened) return;

                Send(data, async);
            }
        }

        #endregion
    }
}
