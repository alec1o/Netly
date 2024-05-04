using System;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server : ITCP.Server
        {
            private readonly ServerTo _to;
            private readonly ServerOn _on;
            private readonly string _id;
            private readonly bool _isFraming;

            public string Id => _id;
            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public bool IsFraming => _isFraming;
            public X509Certificate Certificate => _to.Certificate;
            public SslProtocols EncryptionProtocol => _to.EncryptionProtocol;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITCP.ServerTo To => _to;
            public ITCP.ServerOn On => _on;
            public ITCP.Client[] Clients => _to.Clients.Values.ToArray();

            private Server()
            {
                _id = Guid.NewGuid().ToString();
                _on = new ServerOn();
            }

            public Server(bool isFraming = true) : this()
            {
                _isFraming = isFraming;
                _to = new ServerTo(this);
            }
        }
    }
}