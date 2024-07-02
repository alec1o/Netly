using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public partial class RUDP
    {
        private class ServerTo : IRUDP.ServerTo
        {
            public Host Host { get; private set; }
            public bool IsOpened { get; private set; }
            private readonly Server _server;
            
            public ServerTo(Server server)
            {
                _server = server;
            }

            public IRUDP.Client[] GetClients()
            {
                throw new NotImplementedException();
            }

            public Task Open(Host host)
            {
                throw new NotImplementedException();
            }

            public Task Close()
            {
                throw new NotImplementedException();
            }

            public void DataBroadcast(byte[] data, MessageType messageType)
            {
                throw new NotImplementedException();
            }

            public void DataBroadcast(string data, MessageType messageType)
            {
                throw new NotImplementedException();
            }

            public void DataBroadcast(string data, MessageType messageType, Encoding encoding)
            {
                throw new NotImplementedException();
            }

            public void EventBroadcast(string name, byte[] data, MessageType messageType)
            {
                throw new NotImplementedException();
            }

            public void EventBroadcast(string name, string data, MessageType messageType)
            {
                throw new NotImplementedException();
            }

            public void EventBroadcast(string name, string data, MessageType messageType, Encoding encoding)
            {
                throw new NotImplementedException();
            }
        }
    }
}