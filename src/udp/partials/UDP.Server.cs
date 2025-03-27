using System;
using System.Linq;
using Netly.Interfaces;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server : IUDP.Server
        {
            private readonly ServerOn _on;
            private readonly ServerTo _to;

            public Server()
            {
                Id = Guid.NewGuid().ToString();
                _on = new ServerOn();
                _to = new ServerTo(this);
            }

            public string Id { get; }
            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public IUDP.ServerTo To => _to;
            public IUDP.ServerOn On => _on;
            public IUDP.Client[] Clients => _to.Clients.Select(x => (IUDP.Client)x).ToArray();
        }
    }
}