using System;
using System.Net.Sockets;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client : IRUDP.Client
        {
            internal readonly ClientOn _on;
            private readonly ClientTo _to;

            public Client()
            {
                _on = new ClientOn();
                _to = new ClientTo(this);
            }

            // server side client
            internal Client(Connection connection, Host host)
            {
                _on = new ClientOn();
                _to = new ClientTo(this, connection, host);
            }

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public IRUDP.ClientTo To => _to;
            public IRUDP.ClientOn On => _on;
            public string Id => _to.Id;

            public int HandshakeTimeout
            {
                get => _to.GetHandshakeTimeout();
                set => _to.SetHandshakeTimeout(value);
            }

            public int NoResponseTimeout
            {
                get => _to.GetNoResponseTimeout();
                set => _to.SetNoResponseTimeout(value);
            }

        }
    }
}