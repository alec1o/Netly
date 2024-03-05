using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server
        {
            internal class _To : ITo
            {
                private readonly Server _server;
                public bool IsOpened { get; private set; }
                public Host Host { get; private set; }
                public bool IsFraming => _server._isFraming;
                public bool IsEncrypted { get; private set; }
                public X509Certificate2 Certificate { get; private set; }
                public SslProtocols EncryptionProtocol { get; private set; }
                public List<IClient> Clients { get; private set; }

            public _To(Server server)
                {
                    _server = server;
                }

                public void Open(Host host)
                {
                    throw new NotImplementedException();
                }

                public void Close()
                {
                    throw new NotImplementedException();
                }

                public void Encryption(bool enable, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}