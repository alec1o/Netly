using System;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client : IClient
        {
            private readonly _On _on;
            private readonly _To _to;
            private readonly string _id;
            private readonly bool _useConnection;
            private readonly int _connectionTimeout;

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public ITo To => _to;
            public IOn On => _on;
            public string Id => _id;
            public bool UseConnection => _useConnection;
            public int ConnectionTimeout => _connectionTimeout;


            private Client()
            {
                _on = new _On();
                _to = new _To(this);
                _id = Guid.NewGuid().ToString();
            }

            public Client(bool useConnection = true, int connectionTimeout = 5000) : this()
            {
                _useConnection = useConnection;
                _connectionTimeout = connectionTimeout;
            }

            internal Client(IServer server, Host host, out bool success) : this()
            {
                _useConnection = server.UseConnection;
                _to = new _To(this, host, out success);
            }

            internal void InitServerSide() => _to.InitServerSide();
        }
    }
}