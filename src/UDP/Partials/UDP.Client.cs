using System;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client : IClient
        {
            private readonly ClientOn _on;
            private readonly ClientTo _to;
            private readonly string _id;

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public IClientTo To => _to;
            public IClientOn On => _on;
            public string Id => _id;

            public Client()
            {
                _on = new ClientOn();
                _to = new ClientTo(this);
                _id = Guid.NewGuid().ToString();
            }

            internal Client(IServer server, Host host, out bool success) : this()
            {
                _to = new ClientTo(this, host, out success);
            }

            internal void InitServerSide() => _to.InitServerSide();
        }
    }
}