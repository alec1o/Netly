using System;
using System.Net.Sockets;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client : ITCP.Client
        {
            private readonly ClientOn _on;
            private readonly ClientTo _to;
            private readonly string _id;
            private readonly bool _isFraming;

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITCP.ClientTo To => _to;
            public ITCP.ClientOn On => _on;
            public string Id => _id;
            public bool IsFraming => _isFraming;


            private Client()
            {
                _on = new ClientOn();
                _to = new ClientTo(this);
                _id = Guid.NewGuid().ToString();
            }

            public Client(bool isFraming = true) : this()
            {
                _isFraming = isFraming;
            }

            internal Client(Socket socket, ITCP.Server server, Action<Client,  bool> serverValidatorCallback) : this()
            {
                _isFraming = server.IsFraming;
                _to = new ClientTo(this, socket, server, serverValidatorCallback);
            }

            internal void InitServerValidator() => _to.InitServerValidator();
            internal void InitServerSide() => _to.InitServerSide();
        }
    }
}