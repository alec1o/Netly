using System;
using System.Linq;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Server : IServer
        {
            private readonly _On _on;
            private readonly _To _to;
            public string Id { get; }
            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public bool UseConnection { get; }
            public ITo To => _to;
            public IOn On => _on;
            public IClient[] Clients =>  _to.Clients.Select(x => (IClient)x).ToArray();

            private Server(bool useConnection)
            {
                _on = new _On();
                _to = new _To(this);
                Id = Guid.NewGuid().ToString();
                UseConnection = useConnection;
            }
        }
    }
}