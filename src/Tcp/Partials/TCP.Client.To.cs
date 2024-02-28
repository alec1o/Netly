using System.Net.Security;
using System.Net.Sockets;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client
        {
            internal class _To : ITo
            {
                public Host Host { get; private set; }
                public bool IsEncrypted { get; private set; }
                public bool IsOpened => IsConnected();

                public bool IsFraming => _client._isFraming;

                public string Id => _client._id;

                private Socket _socket;
                private NetworkStream _netStream;
                private SslStream _sslStream;
                private Client _client;
                private Server _server;


                private _To()
                {
                    _socket = null;
                    _netStream = null;
                    _sslStream = null;
                    Host = Host.Default;
                    IsEncrypted = false;
                }

                public _To(Client client, Socket socket, Server server, out bool success) : this()
                {
                    _client = client;
                    _server = server;
                    _socket = socket;
                    success = false;
                }

                public _To(Client client)
                {
                }

                /* ---- INTERFACE --- */

                public void Open(Host host)
                {
                    throw new System.NotImplementedException();
                }

                public void Close()
                {
                    throw new System.NotImplementedException();
                }

                public void Data(byte[] data)
                {
                    throw new System.NotImplementedException();
                }

                public void Encryption(bool enable)
                {
                    throw new System.NotImplementedException();
                }

                public void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    throw new System.NotImplementedException();
                }

                public void Event(string name, byte[] data)
                {
                    throw new System.NotImplementedException();
                }

                public void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8)
                {
                    throw new System.NotImplementedException();
                }

                /* ---- INTERNAL --- */

                private bool IsConnected()
                {
                    try
                    {
                        if (_socket == null || !_socket.Connected) return false;
                        const int timeout = 5000;
                        return !(_socket.Poll(timeout, SelectMode.SelectRead) && _socket.Available == 0);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }
    }
}