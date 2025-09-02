using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client : ITCP.Client
        {
            internal readonly ClientOn _on;
            private readonly ClientTo _to;

            private Client()
            {
                _to = new ClientTo(this);
                _on = new ClientOn();
            }

            public Client(bool isFraming = true) : this()
            {
                IsFraming = isFraming;
            }

            internal Client(Socket socket, Server server, Action<Client> serverValidatorCallback) : this()
            {
                IsFraming = server.IsFraming;
                _to = new ClientTo(this, socket, server, serverValidatorCallback);
            }

            public SslStream SslStream => _to.GetSslStream();
            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public Socket Socket => _to.GetSocket();
            public NetworkStream NetworkStream => _to.GetNetworkStream();
            public bool IsEncrypted => _to.IsEncrypted;
            public ITCP.ClientTo To => _to;
            public ITCP.ClientOn On => _on;
            public string Id { get; } = Guid.NewGuid().ToString();
            public bool IsFraming { get; }

            internal void InitServerValidator()
            {
                _to.InitServerValidator();
            }

            internal void InitServerSide()
            {
                _to.InitServerSide();
            }
        }
    }
}