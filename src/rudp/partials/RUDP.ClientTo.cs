using System;
using System.Text;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientTo : IRUDP.ClientTo
            {
                public Host Host { get; private set; }
                public bool IsOpened { get; private set; }
                private readonly Client _client;

                public ClientTo(Client client)
                {
                    _client = client;
                }
                
                public Task Open(Host host)
                {
                    throw new NotImplementedException();
                }

                public Task Close()
                {
                    throw new NotImplementedException();
                }

                public void Data(byte[] data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                public void Data(string data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                public void Data(string data, MessageType messageType, Encoding encoding)
                {
                    throw new NotImplementedException();
                }

                public void Event(string name, byte[] data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                public void Event(string name, string data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                public void Event(string name, string data, MessageType messageType, Encoding encoding)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}