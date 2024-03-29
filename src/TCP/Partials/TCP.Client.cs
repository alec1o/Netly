﻿using System;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client : IClient
        {
            private readonly _On _on;
            private readonly _To _to;
            private readonly string _id;
            private readonly bool _isFraming;

            public bool IsOpened => _to.IsOpened;
            public Host Host => _to.Host;
            public bool IsEncrypted => _to.IsEncrypted;
            public ITo To => _to;
            public IOn On => _on;
            public string Id => _id;
            public bool IsFraming => _isFraming;


            private Client()
            {
                _on = new _On();
                _to = new _To(this);
                _id = Guid.NewGuid().ToString();
            }

            public Client(bool isFraming = true) : this()
            {
                _isFraming = isFraming;
            }

            internal Client(Socket socket, IServer server, Action<Client,  bool> serverValidatorCallback) : this()
            {
                _isFraming = server.IsFraming;
                _to = new _To(this, socket, server, serverValidatorCallback);
            }

            internal void InitServerValidator() => _to.InitServerValidator();
            internal void InitServerSide() => _to.InitServerSide();
        }
    }
}