using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public partial class RUDP
    {
        public partial class Server
        {
            private class ServerTo : IRUDP.ServerTo
            {
                private readonly Server _server;
                private readonly UDP.Server _udp;

                private ServerTo()
                {
                    _udp = new UDP.Server();
                    Clients = new List<Client>();
                }

                public ServerTo(Server server) : this()
                {
                    _server = server;
                    InitRudpBehave();
                }

                private ServerOn On => _server._on;
                public bool IsOpened => _udp.IsOpened;
                public Host Host => _udp.Host;
                public List<Client> Clients { get; }

                public Task Open(Host host)
                {
                    return _udp.To.Open(host);
                }

                public Task Close()
                {
                    return _udp.To.Close();
                }

                public void DataBroadcast(byte[] data)
                {
                    if (!IsOpened || data == null) return;

                    Broadcast(data);
                }

                public void DataBroadcast(string data)
                {
                    if (!IsOpened || data == null) return;

                    Broadcast(NE.GetBytes(data));
                }

                public void DataBroadcast(string data, Encoding encoding)
                {
                    if (!IsOpened || data == null) return;

                    Broadcast(NE.GetBytes(data, encoding));
                }

                public void EventBroadcast(string name, byte[] data)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Broadcast(EventManager.Create(name, data));
                }

                public void EventBroadcast(string name, string data)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Broadcast(EventManager.Create(name, NE.GetBytes(data)));
                }

                public void EventBroadcast(string name, string data, Encoding encoding)
                {
                    if (!IsOpened || name == null || data == null) return;

                    Broadcast(EventManager.Create(name, NE.GetBytes(data, encoding)));
                }

                private void Broadcast(byte[] data)
                {
                    _udp.To.DataBroadcast(data);
                }

                private void Send(Host host, byte[] bytes)
                {
                    _udp.To.Data(host, bytes);
                }

                private void InitRudpBehave()
                {
                    throw new NotImplementedException(nameof(InitRudpBehave));
                }
            }
        }
    }
}