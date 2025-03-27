﻿using System;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Server : IRUDP.Server
        {
            internal readonly ServerOn _on;
            private readonly ServerTo _to;

            public Server()
            {
                Id = Guid.NewGuid().ToString();
                _on = new ServerOn();
                _to = new ServerTo(this);
            }

            public string Id { get; }
            public int HandshakeTimeout
            {
                get => _to.GetHandshakeTimeout();
                set => _to.SetHandshakeTimeout(value);
            }

            public int NoResponseTimeout
            {
                get => _to.GetNoResponseTimeout();
                set => _to.SetNoResponseTimeout(value);
            }
            public Host Host => _to.Host;
            public bool IsOpened => _to.IsOpened;
            public IRUDP.ServerTo To => _to;
            public IRUDP.ServerOn On => _on;
            public IRUDP.Client[] Clients => _to.GetClients();
        }
    }
}