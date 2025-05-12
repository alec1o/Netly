using System;
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
            internal readonly NetlyEnvironment.Parallelism _parallelism;

            private Server(int threads)
            {
                if (threads <= 1) threads = 1;
                _parallelism = new NetlyEnvironment.Parallelism();
                _on = new ServerOn();
                _on.Open(() => _parallelism?.Start(threads));
                _on.Close(() => _parallelism?.Stop());
                _on.Error(_ => _parallelism?.Stop());
            }

            public Server(bool isFraming = true, int threads = 3) : this(threads)
            {
                IsFraming = isFraming;
                _to = new ServerTo(this);
            }

            public string Id { get; } = Guid.NewGuid().ToString();

            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public bool IsFraming { get; }

            public X509Certificate Certificate => _to.Certificate;
            public SslProtocols EncryptionProtocol => _to.EncryptionProtocol;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITCP.ServerTo To => _to;
            public ITCP.ServerOn On => _on;

            public ITCP.Client[] Clients => _to.Clients.ToArray();
        }
    }
}