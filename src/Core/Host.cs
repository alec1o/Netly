using System.Net;
using System.Net.Sockets;

namespace Netly.Core
{
    /// <summary>
    /// Netly: Host (EndPoint Manager)
    /// </summary>
    public class Host
    {
        /// <summary>
        /// Return IPAddress
        /// </summary>
        public IPAddress Address => _endpoint.Address;

        /// <summary>
        /// Return Port
        /// </summary>
        public int Port => _endpoint.Port;

        /// <summary>
        /// Return EndPoint
        /// </summary>
        public EndPoint EndPoint => _endpoint;

        /// <summary>
        /// Return IPEndPoint
        /// </summary>
        public IPEndPoint IPEndPoint => _endpoint;

        /// <summary>
        /// Return AddressFamily
        /// </summary>
        public AddressFamily AddressFamily => _endpoint.AddressFamily;

        private readonly IPEndPoint _endpoint;

        /// <summary>
        /// Create instance of (host)
        /// </summary>
        /// <param name="ip">IPAddress</param>
        /// <param name="port">Port</param>
        public Host(string ip, int port)
        {
            _endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        /// <summary>
        /// Create instance of (host)
        /// </summary>
        /// <param name="endpoint">EndPoint</param>
        public Host(EndPoint endpoint)
        {
            IPEndPoint temp = (IPEndPoint)endpoint;
            _endpoint = new IPEndPoint(temp.Address, temp.Port);
        }

        /// <summary>
        /// Create instance of (host)
        /// </summary>
        /// <param name="endpoint">IPEndPoint</param>
        public Host(IPEndPoint endpoint)
        {
            _endpoint = new IPEndPoint(endpoint.Address, endpoint.Port);
        }

        /// <summary>
        /// Create instance of (host)
        /// </summary>
        /// <param name="address">IPAddress</param>
        /// <param name="port">Port</param>
        public Host(IPAddress address, int port)
        {
            _endpoint = new IPEndPoint(address, port);
        }

        public override string ToString()
        {
            return _endpoint.ToString();
        }
    }
}
