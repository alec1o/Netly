using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Zenet.Network.Tcp
{
    public class ServerTCP
    {
        public bool Running { get; private set; }
        public Socket Socket => ESocket.Socket;
        public bool Opened => ESocket.IsBind;
        public readonly Host EHost;
        public readonly EasySocket ESocket;
        public readonly int MaxClient;
        public readonly List<AgentTCP> Clients;
        private EventHandler<object> OnOpenEvent, OnErrorEvent, OnCloseEvent, OnClientOpenEvent, OnClientCloseEvent;
        private EventHandler<(AgentTCP client, byte[] data)> OnClientReceiveEvent;
        private bool tryOpen, tryClose;

        public ServerTCP(Host eHost, int maxClient = -1)
        {
            EHost = eHost;
            ESocket = new EasySocket(Protocol.TCP, EHost);
            MaxClient = maxClient;
            Clients = new List<AgentTCP>();
        }

        public void Open(int backlog = 0)
        {
            if (tryOpen || Running) return;
            tryOpen = true;

            ESocket.Bind
            (
                () =>
                {
                    //success
                    tryOpen = false;
                    Running = true;
                    OnOpenEvent?.Invoke(this, null);

                    //accept
                    BeginAccept();
                },
                (e) =>
                {
                    //error
                    tryOpen = false;
                    Running = false;
                    OnErrorEvent?.Invoke(this, e);
                },
                backlog
            );
        }

        private void BeginAccept()
        {
            if (!Opened) return;

            try
            {
                Socket.BeginAccept(EndAccept, null);
            }
            catch
            {
                BeginAccept();
            }
        }

        private void EndAccept(IAsyncResult result)
        {
            var socket = Socket.EndAccept(result);
            var client = new AgentTCP(socket);

            client.OnOpen(() =>
            {
                OnClientOpenEvent?.Invoke(this, client);
            });

            client.OnClose(() =>
            {
                OnClientCloseEvent?.Invoke(this, client);
            });

            client.OnReceive((data) =>
            {
                OnClientReceiveEvent?.Invoke(this, (client, data));
            });

            Clients.Add(client);

            BeginAccept();
        }

        public void Close()
        {
            if (tryClose || !Running) return;
            tryClose = true;

            ESocket.Close(() =>
            {
                foreach(var client in Clients)
                {
                    client?.Close();
                }

                Clients.Clear();

                OnCloseEvent?.Invoke(this, null);
            });
        }

        public void OnOpen(Action callback)
        {
            OnOpenEvent += (_, e) =>
            {
                Callback.Execute(() =>
                {
                    Callback.Execute(() => callback?.Invoke());
                });
            };
        }

        public void OnError(Action<Exception> callback)
        {
            OnErrorEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e as Exception));
            };
        }

        public void OnClose(Action callback)
        {
            OnCloseEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke());
            };
        }

        public void OnClientOpen(Action<AgentTCP> callback)
        {
            OnClientOpenEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e as AgentTCP));
            };
        }
        public void OnClientClose(Action<AgentTCP> callback)
        {
            OnClientCloseEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e as AgentTCP));
            };
        }

        public void OnClientReceive(Action<AgentTCP, byte[]> callback)
        {
            OnClientReceiveEvent += (_, e) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(e.client, e.data);
                });
            };
        }
    }
}
