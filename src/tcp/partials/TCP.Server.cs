using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

            public Server(bool isFraming = true)
            {
                Id = Guid.NewGuid().ToString();
                IsFraming = isFraming;
                Framing = new Framing(IsFraming);
                _on = new ServerOn();
                _to = new ServerTo(this);
            }

            public string Id { get; }
            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public bool IsFraming { get; }

            public IFraming Framing { get; }
            public X509Certificate Certificate => _to.Certificate;
            public SslProtocols EncryptionProtocol => _to.EncryptionProtocol;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITCP.ServerTo To => _to;
            public ITCP.ServerOn On => _on;

            public List<ITCP.Client> Clients => _to.Clients;
            public Socket Socket => _to.GetSocket();
        }
    }
}