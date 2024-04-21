using System;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client : IRUDP.Client
        {
            private readonly ClientOn _on;
            private readonly ClientTo _to;

            public Client()
            {
                Id = Guid.NewGuid().ToString();
                _on = new ClientOn();
                _to = new ClientTo(this);
            }

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public IRUDP.ClientTo To => _to;
            public IRUDP.ClientOn On => _on;
            public string Id { get; }
        }
    }
}