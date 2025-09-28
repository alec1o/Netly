using System;
using System.Net.Sockets;
using Netly.Interfaces;

namespace Netly
{
    public static partial class UDP
    {
        public partial class Client : IUDP.Client
        {
            private readonly ClientOn _on;
            private readonly ClientTo _to;


            public Client()
            {
                Id = Guid.NewGuid().ToString();
                _on = new ClientOn();
                _to = new ClientTo(this);
            }

            internal Client(ref NHost host, ref Socket socket) : this()
            {
                _to = new ClientTo(this, ref host, ref socket);
            }

            public bool IsOpened => _to.IsOpened;
            public NHost Host => _to.Host;
            public IUDP.ClientTo To => _to;
            public IUDP.ClientOn On => _on;
            public string Id { get; }

            internal void InitServerSide()
            {
                _to.InitServerSide();
            }

            internal void OnServerBuffer(ref byte[] buffer)
            {
                // TODO: Will cause error +2GB
                if (buffer.LongLength > int.MaxValue) throw new IndexOutOfRangeException(nameof(buffer.LongLength));
                
                var stream = NHelper.NewStream(buffer.LongLength);
                stream.Write(buffer, 0, buffer.Length);
                _to.OnServerBuffer(stream);
            }
        }
    }
}