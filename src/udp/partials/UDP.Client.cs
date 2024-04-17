using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client : Interfaces.UDP.IClient
        {
            private readonly ClientOn _on;
            private readonly ClientTo _to;


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

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public Interfaces.UDP.IClientTo To => _to;
            public Interfaces.UDP.IClientOn On => _on;
            public string Id { get; }

            internal void InitServerSide()
            {
                _to.InitServerSide();
            }

            internal void OnServerBuffer(ref byte[] buffer)
            {
                _to.OnServerBuffer(ref buffer);
            }
        }
    }
}