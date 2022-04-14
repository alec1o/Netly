using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Zenet.Network.Tcp
{
    public class ServerTCP
    {
        public static int Backlog = 0;
        public bool Opened { get; private set; }
        public List<AgentTCP> Clients;
        public readonly Socket Socket;
        public readonly Host Host;
        private Socket socket;
        private bool tryOpen, tryClose;

        private EventHandler OnOpenEvent;
        private EventHandler OnCloseEvent;
        private EventHandler<Exception> OnErrorEvent;

        private EventHandler<AgentTCP> OnClientOpenEvent;
        private EventHandler<AgentTCP> OnClientCloseEvent;
        private EventHandler<(AgentTCP client, byte[] data)> OnClientDataEvent;
        
        public ServerTCP(Host host, Socket socket = null)
        {
            Host = host;
            Socket = socket ?? new Socket(host.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Clients = new List<AgentTCP>();
        }

        public void Open()
        {
            if (tryOpen || Opened) return;
            tryOpen = true;

            socket = EasySocket.CopySocket(Socket);

            Async.Thread(() =>
            {
                try
                {
                    socket.Bind(Host.EndPoint);
                    socket.Listen(Backlog);

                    Opened = true;
                    OnOpenEvent?.Invoke(this, null);

                    BeginAccept();
                }
                catch (Exception e)
                {
                    OnErrorEvent?.Invoke(this, e);
                }

                tryOpen = false;
            });
        }

        private void BeginAccept()
        {
            if (!Opened) return;

            try
            {
                socket.BeginAccept(EndAccept, null);
            }
            catch
            {
                BeginAccept();
            }
        }

        private void EndAccept(IAsyncResult result)
        {
            var client = socket.EndAccept(result);

            var agent = new AgentTCP(ref client);

            agent.OnOpen(() =>
            {
                Clients.Add(agent);
                OnClientOpenEvent?.Invoke(this, agent);
            });

            agent.OnClose(() =>
            {
                Clients.Remove(agent);
                OnClientCloseEvent?.Invoke(this, agent);
            });

            agent.OnReceive((data) => OnClientDataEvent?.Invoke(this, (agent, data)));

            BeginAccept();
        }

        public void OnOpen(Action callback)
        {
            OnOpenEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke());
            };
        }
        
        public void OnClose(Action callback)
        {
            OnCloseEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke());
            };
        }
        
        public void OnError(Action<Exception> callback)
        {
            OnErrorEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e));
            };
        }
        
        public void OnClientOpen(Action<AgentTCP> callback)
        {
            OnClientOpenEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e));
            };
        }
        
        public void OnClientClose(Action<AgentTCP> callback)
        {
            OnClientCloseEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e));
            };
        }
        
        public void OnClientReceive(Action<AgentTCP, byte[]> callback)
        {
            OnClientDataEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e.client, e.data));
            };
        }

        public void Close()
        {
            if (tryClose || !Opened) return;

            socket.Shutdown(SocketShutdown.Both);

            Async.Thread(() =>
            {
                socket.Close();

                foreach (var client in Clients.ToArray())
                {
                    client?.Close();
                }

                Clients.Clear();

                Opened = false;
                tryClose = false;
                OnCloseEvent?.Invoke(this, null);
            });
        }
    }
}