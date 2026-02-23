using System;
using System.Net;
using System.Net.Sockets;

namespace Netly
{
    /// <summary>
    ///     Represents a network host (IP address + port) and provides multiple constructors and implicit conversions
    ///     from string. Supports equality comparison and conversion to <see cref="IPEndPoint" />.
    /// </summary>
    public class NHost
    {
        /// <summary>
        ///     Default host instance representing 0.0.0.0:0.
        /// </summary>
        public static readonly NHost Default = new NHost(IPAddress.Any, 0);

        /// <summary>
        ///     Creates a new <see cref="NHost" /> from an IP string and port number.
        /// </summary>
        /// <param name="ip">The IP address as a string.</param>
        /// <param name="port">The port number.</param>
        /// <exception cref="FormatException">Thrown if the IP string is not a valid IP address.</exception>
        public NHost(string ip, int port)
        {
            if (!IPAddress.TryParse(ip, out var address))
                throw new FormatException($"Invalid IP address: {ip}");

            IPEndPoint = new IPEndPoint(address, port);
        }

        /// <summary>
        ///     Creates a new <see cref="NHost" /> from an <see cref="EndPoint" />.
        /// </summary>
        /// <param name="endpoint">The endpoint to copy.</param>
        public NHost(EndPoint endpoint)
        {
            var temp = endpoint as IPEndPoint ?? throw new ArgumentException("EndPoint must be IPEndPoint");
            IPEndPoint = new IPEndPoint(temp.Address, temp.Port);
        }

        /// <summary>
        ///     Creates a new <see cref="NHost" /> from an <see cref="IPEndPoint" />.
        /// </summary>
        /// <param name="ipEndpoint">The IPEndPoint to copy.</param>
        public NHost(IPEndPoint ipEndpoint)
        {
            IPEndPoint = new IPEndPoint(ipEndpoint.Address, ipEndpoint.Port);
        }

        /// <summary>
        ///     Creates a new <see cref="NHost" /> from an <see cref="IPAddress" /> and port.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="port">The port number.</param>
        public NHost(IPAddress address, int port)
        {
            IPEndPoint = new IPEndPoint(address, port);
        }

        /// <summary>
        ///     Returns the <see cref="IPAddress" /> of the host.
        /// </summary>
        public IPAddress Address => IPEndPoint.Address;

        /// <summary>
        ///     Returns the port number of the host.
        /// </summary>
        public int Port => IPEndPoint.Port;

        /// <summary>
        ///     Returns the host as an <see cref="EndPoint" />.
        /// </summary>
        public EndPoint EndPoint => IPEndPoint;

        /// <summary>
        ///     Returns the <see cref="IPEndPoint" /> representing this host.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Returns the address family of the host.
        /// </summary>
        public AddressFamily AddressFamily => IPEndPoint.AddressFamily;

        /// <summary>
        ///     Returns a string representation of the host as "IP:Port".
        /// </summary>
        public override string ToString()
        {
            return IPEndPoint.ToString();
        }

        /// <summary>
        ///     Determines whether the specified object is equal to the current host by comparing IP and port.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object obj)
        {
            return obj is NHost other && Address.Equals(other.Address) && Port == other.Port;
        }

        /// <summary>
        ///     Returns a hash code for this host.
        /// </summary>
        public override int GetHashCode()
        {
            return IPEndPoint == null ? 0 : IPEndPoint.GetHashCode();
        }

        /// <summary>
        ///     Implicitly converts a valid IP:Port string to an <see cref="NHost" />.
        /// </summary>
        /// <param name="host">The host string (e.g., "127.0.0.1:3300").</param>
        /// <exception cref="FormatException">Thrown if the string is not a valid IP:Port format.</exception>
        public static implicit operator NHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new FormatException("Host string is null or empty.");

            // Add temporary scheme for Uri parsing
            var temp = host.Contains("://") ? host : "tcp://" + host;

            Uri uri;
            try
            {
                uri = new Uri(temp);
            }
            catch
            {
                throw new FormatException($"Invalid host format: {host}");
            }

            if (!IPAddress.TryParse(uri.Host, out _))
                throw new FormatException($"Invalid IP address in host: {host}");

            return new NHost(uri.Host, uri.Port);
        }
    }
}