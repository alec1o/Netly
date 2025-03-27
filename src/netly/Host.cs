using System.Net;
using System.Net.Sockets;

namespace Netly
{
    /// <summary>
    ///     Netly: Host (EndPoint Manager)
    /// </summary>
    public class Host
    {
        /// <summary>
        ///     Return default Host instance: (0.0.0.0:0)
        /// </summary>
        /// <returns>Return default host instance: (0.0.0.0:0)</returns>
        public static readonly Host Default = new Host(IPAddress.Any, 0);

        /// <summary>
        ///     Create instance of (host)
        /// </summary>
        /// <param name="ip">IPAddress</param>
        /// <param name="port">Port</param>
        public Host(string ip, int port)
        {
            IPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        /// <summary>
        ///     Create instance of (host)
        /// </summary>
        /// <param name="endpoint">EndPoint</param>
        public Host(EndPoint endpoint)
        {
            var temp = (IPEndPoint)endpoint;
            IPEndPoint = new IPEndPoint(temp.Address, temp.Port);
        }

        /// <summary>
        ///     Create instance of (host)
        /// </summary>
        /// <param name="ipEndpoint">IPEndPoint</param>
        public Host(IPEndPoint ipEndpoint)
        {
            IPEndPoint = new IPEndPoint(ipEndpoint.Address, ipEndpoint.Port);
        }

        /// <summary>
        ///     Create instance of (host)
        /// </summary>
        /// <param name="address">IPAddress</param>
        /// <param name="port">Port</param>
        public Host(IPAddress address, int port)
        {
            IPEndPoint = new IPEndPoint(address, port);
        }

        /// <summary>
        ///     Return IPAddress
        /// </summary>
        public IPAddress Address => IPEndPoint.Address;

        /// <summary>
        ///     Return Port
        /// </summary>
        public int Port => IPEndPoint.Port;

        /// <summary>
        ///     Return EndPoint
        /// </summary>
        public EndPoint EndPoint => IPEndPoint;

        /// <summary>
        ///     Return IPEndPoint
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Return AddressFamily
        /// </summary>
        public AddressFamily AddressFamily => IPEndPoint.AddressFamily;

        public override string ToString()
        {
            return IPEndPoint.ToString();
        }

        /// <summary>
        ///     Compare Host. Check IP/Port
        /// </summary>
        /// <param name="object">Object</param>
        /// <returns></returns>
        public override bool Equals(object @object)
        {
            return Equals(this, @object);
        }

        /// <summary>
        ///     Compare two (2) Host. Check IP/Port
        /// </summary>
        /// <param name="objectA">Object A</param>
        /// <param name="objectB">Object B</param>
        /// <returns>Return true if those object is Host and have same value</returns>
        public new static bool Equals(object objectA, object objectB)
        {
            if (objectA == null || objectB == null) return false;

            if (objectA.GetType() == typeof(Host) && objectB.GetType() == typeof(Host))
                return ((Host)objectA).ToString() == ((Host)objectB).ToString();

            return false;
        }

        public override int GetHashCode()
        {
            if (IPEndPoint == null) return 0;
            return IPEndPoint.GetHashCode();
        }
    }
}