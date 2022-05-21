using System;
using System.Collections.Generic;
using System.Text;
using Zenet.Package;
using Zenet.Network;
using System.Net.Sockets;

namespace Zenet.Tcp
{
    public class TcpServer : IServer
    {
        public Socket Socket => throw new NotImplementedException();

        public Host Host => throw new NotImplementedException();

        public bool Opened => throw new NotImplementedException();

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void OnClose(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnData(Action<object, byte[]> callback)
        {
            throw new NotImplementedException();
        }

        public void OnEnter(Action<object> callback)
        {
            throw new NotImplementedException();
        }

        public void OnError(Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public void OnEvent(Action<object, string, byte[]> callback)
        {
            throw new NotImplementedException();
        }

        public void OnExit(Action<object> callback)
        {
            throw new NotImplementedException();
        }

        public void OnOpen(Action callback)
        {
            throw new NotImplementedException();
        }

        public void Open(Host host)
        {
            throw new NotImplementedException();
        }
    }
}
