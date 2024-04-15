using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public partial class UDP
    {
        public partial class Server 
        {
            public class ServerTo : UDP.IServerTo
            {
                private readonly Server _server;
                public bool IsOpened { get; set; }
                public Host Host { get; set; }
                public List<Client> Clients { get; set; }

                public ServerTo(Server server)
                {
                    _server = server;
                    IsOpened = false;
                    Host = new Host(IPAddress.Any, 0);
                    Clients = new List<Client>();
                }

                public Task Open(Host host)
                {
                    throw new System.NotImplementedException();
                }

                public Task Close()
                {
                    throw new System.NotImplementedException();
                }

                public Task BroadcastData(byte[] data)
                {
                    throw new System.NotImplementedException();
                }

                public Task BroadcastData(string data, NE.Encoding encoding)
                {
                    throw new System.NotImplementedException();
                }

                public Task BroadcastEvent(string name, byte[] data)
                {
                    throw new System.NotImplementedException();
                }

                public Task BroadcastEvent(string name, string data, NE.Encoding encoding)
                {
                    throw new System.NotImplementedException();
                }
            }
        }
    }
}