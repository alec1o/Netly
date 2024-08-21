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

            public Client(Host host, Socket socket) : this()
            {
                _to = new ClientTo(this, host, socket);
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

            internal void InjectBuffer(ref byte[] bytes)
            {
                _to.InjectBuffer(ref bytes);
            }

            internal void StartServerSideConnection(Action<bool> callback)
            {
                _to.StartServerSideConnection(ref callback);
            }
        }
    }
}