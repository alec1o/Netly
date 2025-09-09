using System;
using System.Net.Security;
using System.Net.Sockets;
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
                Framing = new Framing(isFraming);
            }

            internal Client(Socket socket, Server server, Action<Client> serverValidatorCallback) : this()
            {
                IsFraming = server.IsFraming;
                _to = new ClientTo(this, socket, server, serverValidatorCallback);
                Framing = new Framing(server.Framing);
            }

            public SslStream SslStream => _to.GetSslStream();
            public IFraming Framing { get; }
            public bool IsOpened => _to.IsOpened;
            public NHost Host => _to.Host;
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