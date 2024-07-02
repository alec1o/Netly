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
                    Send(null, data, messageType);
                }

                public void Data(string data, MessageType messageType)
                {
                    Send(null, data.GetBytes(), messageType);
                }

                public void Data(string data, MessageType messageType, Encoding encoding)
                {
                    Send(null, data.GetBytes(encoding), messageType);
                }

                public void Event(string name, byte[] data, MessageType messageType)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data), messageType);
                }

                public void Event(string name, string data, MessageType messageType)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data.GetBytes()), messageType);
                }

                public void Event(string name, string data, MessageType messageType, Encoding encoding)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)), messageType);
                }

                private void Send(Host host, byte[] data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                private void SendRaw(Host host, ref byte[] data)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}