using System;
using System.Collections.Generic;
using Zenet.Network;
using System.Net.Sockets;
using System.Net;

namespace Zenet.Tcp
{
    public class TcpServer : IServer
    {
        #region Private

        private Socket _socket { get; set; }
        private Host _host { get; set; }

        private bool _tryOpen { get; set; }
        private bool _tryClose { get; set; }
        private bool _opened { get; set; }

        private EventHandler _OnOpen { get; set; }
        private EventHandler<Exception> _OnError { get; set; }
        private EventHandler _OnClose { get; set; }
        private EventHandler<object> _OnEnter { get; set; }
        private EventHandler<object> _OnExit { get; set; }
        private EventHandler<(object client, byte[] data)> _OnData { get; set; }
        private EventHandler<(object client, string name, byte[] data)> _OnEvent { get; set; }

        #endregion

        #region Public

        public Socket Socket => _socket;

        public Host Host => _host;

        public bool Opened => VerifyOpened();

        public List<object> Clients { get; private set; }

        #endregion

        #region Init

        public TcpServer()
        {
            _host = new Host(IPAddress.Any, 0);
            _socket = new Socket(_host.Family, SocketType.Stream, ProtocolType.Tcp);
            Clients = new List<object>();
        }


        #endregion

        #region Func

        private bool VerifyOpened()
        {
            if (_socket == null) return false;

            return _opened;
        }

        private void BeginAccept()
        {

        }

        private void EndAccept(IAsyncResult result)
        {

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

        #region Client

        public void OnEnter(Action<object> callback)
        {
            _OnEnter += (_, client) =>
            {
                Callback.Execute(() =>
                {
                    callback?.Invoke(client);
                });
            };
        }

        public void OnExit(Action<object> callback)
        {
            throw new NotImplementedException();
        }

        public void OnData(Action<object, byte[]> callback)
        {
            throw new NotImplementedException();
        }

        public void OnEvent(Action<object, string, byte[]> callback)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Events

        public void OnOpen(Action callback)
        {
            throw new NotImplementedException();
        }

        public void OnError(Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public void OnClose(Action callback)
        {
            throw new NotImplementedException();
        }

        #endregion        
    }
}
