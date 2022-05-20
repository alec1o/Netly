using System;
using System.Net;
using System.Net.Sockets;
using Zenet.Network;

namespace Zenet.Tcp
{
    public class TcpClient : IClient
    {
        #region Private

        private Socket _socket;
        private Host _host;

        #endregion

        #region Public

        public Socket Socket => _socket;

        public Host Host => _host;

        public bool Opened => VerifyOpened();

        #endregion

        #region Init

        public TcpClient()
        {
            _host = new Host(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
        }

        #endregion

        #region Func

        private bool VerifyOpened()
        {
            return false;
        }

        #endregion

        #region Remote

        public void Open(Host host)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Send

        public void ToData(byte[] data)
        {
            throw new NotImplementedException();
        }

        public void ToEvent(string name, byte[] data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Events        

        public void OnOpen(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnError(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnClose(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnData(Action<byte[]> callback)
        {
            throw new NotImplementedException();
        }        

        public void OnEvent(Action<string, byte[]> callback)
        {
            throw new NotImplementedException();
        }        

        #endregion
    }
}
