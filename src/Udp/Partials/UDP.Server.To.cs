﻿using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public partial class UDP
    {
        public partial class Server
        {
            public class _To : ITo
            {
                private bool UseConnection => _server.UseConnection;
                private readonly Server _server;
                public bool IsOpened { get; set; }
                public Host Host { get; set; }
                public List<Client> Clients { get; set; }

                public _To(Server server)
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
            }
        }
    }
}