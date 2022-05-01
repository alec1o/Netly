using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Zeent.Package;

using Zenet.Network;
using Zenet.Package;

namespace Zenet.Udp
{
    public class ServerUDP
    {
        public static readonly byte[] CloseEventData = Event2.Create("408", Encoding2.Bytes("Request timeout"));
        public Host Host { get; private set; }
        public Socket Socket => ESocket.Socket;

        public readonly EasySocket ESocket;
        private readonly int Timeout;
        public readonly int MaxClient;
        public readonly List<AgentUDP> Clients;

        public bool Opened => ESocket.IsBind;
        private bool tryOpen, tryClose;

        private EventHandler<object> OnOpenEvent, OnErrorEvent, OnCloseEvent, OnClientOpenEvent, OnClientCloseEvent;
        private EventHandler<(AgentUDP client, byte[] data)> OnClientDataEvent;

        public ServerUDP(int clientTimeout = -1, int maxClient = -1)
        {
            Timeout = clientTimeout;
            MaxClient = maxClient;
            Clients = new List<AgentUDP>();
            ESocket = new EasySocket(Protocol.UDP);
        }

        public void Open(Host host)
        {
            if (tryOpen || Opened) return;
            tryOpen = true;
            Host = host;

            ESocket.Bind
            (
                Host,
                () =>
                {
                    //success
                    tryOpen = false;
                    OnOpenEvent?.Invoke(this, null);

                    //accept
                    Accept();
                },
                (e) =>
                {
                    //error
                    tryOpen = false;
                    OnErrorEvent?.Invoke(this, e);
                }                
            );
        }

        private void Accept()
        {
            Async.Thread(() =>
            {
                EndPoint host = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = new byte[1024 * 8];
                while (Opened)
                {
                    try
                    {
                        var length = Socket.ReceiveFrom(buffer, ref host);

                        var agent = GetAgent(host as IPEndPoint);

                        if (length > 0)
                        {
                            var data = new byte[length];

                            Buffer.BlockCopy(buffer, 0, data, 0, data.Length);

                            if (data.Length == ServerUDP.CloseEventData.Length && Compare.Bytes(data, ServerUDP.CloseEventData))
                            {
                                agent?.Close();
                                continue;
                            }

                            if (agent != null)
                            {
                                AgentUDP.SetData(data, agent);
                            }
                            else
                            {
                                if (MaxClient > 0 && Clients.Count >= MaxClient) continue;

                                var client = new AgentUDP(this, new Host(host), Timeout);

                                client.OnOpen(() =>
                                {
                                    OnClientOpenEvent?.Invoke(this, client);
                                });

                                client.OnClose(() =>
                                {
                                    foreach (var target in Clients.ToArray())
                                    {
                                        if (client.Id == target.Id) Clients.Remove(target);
                                    }

                                    OnClientCloseEvent?.Invoke(this, client);
                                });

                                client.OnData((target) =>
                                {
                                    OnClientDataEvent?.Invoke(this, (client, target)); ;
                                });

                                Clients.Add(client);
                                AgentUDP.SetData(data, client);
                            }
                        }
                        else
                        {
                            if (agent != null)
                            {
                                agent.Close();
                            }
                        }
                    }
                    catch { }
                }
            });
        }

        private AgentUDP GetAgent(IPEndPoint host)
        {
            foreach(var client in Clients.ToArray())
            {
                try
                {
                    if(client.Host.Port == host.Port && client.Host.IPAddress == host.Address)
                    {
                        return client;
                    }
                }
                catch { }
            }

            return null;
        }

        public void Close()
        {
            if (tryClose || !Opened) return;
            tryClose = true;

            ESocket.Close(() =>
            {
                tryClose = false;

                foreach (var client in Clients)
                {
                    client?.Close();
                }

                Clients.Clear();

                OnCloseEvent?.Invoke(this, null);
            });
        }

        #region Event

        public void OnOpen(Action callback)
        {
            OnOpenEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke());
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

        public void OnClientOpen(Action<AgentUDP> callback)
        {
            OnClientOpenEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e as AgentUDP));
            };
        }
        public void OnClientClose(Action<AgentUDP> callback)
        {
            OnClientCloseEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e as AgentUDP));
            };
        }

        public void OnClientData(Action<AgentUDP, byte[]> callback)
        {
            OnClientDataEvent += (_, e) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(e.client, e.data);
                });
            };
        }

        #endregion
    }
}
