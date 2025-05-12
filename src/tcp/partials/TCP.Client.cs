using System;
using System.Collections.Generic;
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
            internal readonly NetlyEnvironment.Parallelism _parallelism;

            private Client()
            {
                _to = new ClientTo(this);
                _on = new ClientOn();
            }

            public Client(bool isFraming = true) : this()
            {
                IsFraming = isFraming;
                _parallelism = new NetlyEnvironment.Parallelism();
                _on.Open(() => _parallelism?.Start(1));
                _on.Close(() => _parallelism?.Stop());
                _on.Error(_ => _parallelism?.Stop());
            }

            internal Client(Socket socket, Server server, Action<Client, bool> serverValidatorCallback) : this()
            {
                IsFraming = server.IsFraming;
                _parallelism = server._parallelism;
                _to = new ClientTo(this, socket, server, serverValidatorCallback);
            }

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
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