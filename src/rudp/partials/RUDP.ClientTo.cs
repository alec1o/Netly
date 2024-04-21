using System;
using System.Text;
using System.Threading.Tasks;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientTo : IRUDP.ClientTo
            {
                private readonly Client _client;
                private readonly UDP.Client _udp;

                private ClientTo()
                {
                    _udp = new UDP.Client();
                }

                public ClientTo(Client client) : this()
                {
                    _client = client;
                    InitRudpBehave();
                }

                public Host Host => _udp.Host;
                public bool IsOpened => _udp.IsOpened;
                private ClientOn On => _client._on;

                public Task Open(Host host)
                {
                    return _udp.To.Open(host);
                }

                public Task Close()
                {
                    return _udp.To.Close();
                }

                public void Data(byte[] data)
                {
                    _udp.To.Data(data);
                }

                public void Data(string data)
                {
                    _udp.To.Data(data);
                }

                public void Data(string data, Encoding encoding)
                {
                    _udp.To.Data(data, encoding);
                }

                public void Event(string name, byte[] data)
                {
                    _udp.To.Event(name, data);
                }

                public void Event(string name, string data)
                {
                    _udp.To.Event(name, data);
                }

                public void Event(string name, string data, Encoding encoding)
                {
                    _udp.To.Event(name, data, encoding);
                }

                private void InitRudpBehave()
                {
                    throw new NotImplementedException(nameof(InitRudpBehave));
                }
            }
        }
    }
}