using System;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server : ITCP.Server
        {
            private readonly ServerOn _on;
            private readonly ServerTo _to;

            private Server()
            {
                Id = Guid.NewGuid().ToString();
                _on = new ServerOn();
            }

            public Server(bool isFraming = true) : this()
            {
                IsFraming = isFraming;
                _to = new ServerTo(this);
            }

            public string Id { get; }

            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public bool IsFraming { get; }

            public X509Certificate Certificate => _to.Certificate;
            public SslProtocols EncryptionProtocol => _to.EncryptionProtocol;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITCP.ServerTo To => _to;
            public ITCP.ServerOn On => _on;
            public ITCP.Client[] Clients => _to.Clients.Values.ToArray();
        }
    }
}