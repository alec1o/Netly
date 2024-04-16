using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client : IClient
        {
            private readonly ClientOn _on;
            private readonly ClientTo _to;

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public IClientTo To => _to;
            public IClientOn On => _on;
            public string Id { get; }


            public Client()
            {
                Id = Guid.NewGuid().ToString();
                _on = new ClientOn();
                _to = new ClientTo(this);
            }

            internal Client(ref Host host, ref Socket socket) : this()
            {
                _to = new ClientTo(this, ref host, ref socket);
            }

            internal void InitServerSide() => _to.InitServerSide();
            internal void OnServerBuffer(ref byte[] buffer) => _to.OnServerBuffer(ref buffer);
        }
    }
}