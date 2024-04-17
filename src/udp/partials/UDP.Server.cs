using System;
using System.Linq;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server : Interfaces.UDP.IServer
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
            public Interfaces.UDP.IServerTo To => _to;
            public Interfaces.UDP.IServerOn On => _on;
            public Interfaces.UDP.IClient[] Clients => _to.Clients.Select(x => (Interfaces.UDP.IClient)x).ToArray();
        }
    }
}