using System;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server : IServer
        {
            private readonly _To _to;
            private readonly _On _on;
            private readonly string _id;
            private readonly bool _isFraming;

            public string Id => _id;
            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public bool IsFraming => _isFraming;
            public X509Certificate2 Certificate => _to.Certificate;
            public SslProtocols EncryptionProtocol => _to.EncryptionProtocol;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITo To => _to;
            public IOn On => _on;
            public IClient[] Clients => _to.Clients.Values.ToArray();

            private Server()
            {
                _id = Guid.NewGuid().ToString();
                _on = new _On();
            }

            public Server(bool isFraming = true) : this()
            {
                _isFraming = isFraming;
                _to = new _To(this);
            }
        }
    }
}