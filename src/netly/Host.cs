using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Netly
{
    /// <summary>
    ///     Represents a network host (IP address + port) and provides multiple constructors and implicit conversions
    ///     from string. Supports equality comparison and conversion to <see cref="IPEndPoint" />.
    /// </summary>
    public class Host
    {
        /// <summary>
        ///     Default host instance representing 0.0.0.0:0.
        /// </summary>
        public static readonly Host Default = new Host(IPAddress.Any, 0);

        /// <summary>
        ///     Creates a new <see cref="Host" /> from an IP string and port number.
        /// </summary>
        /// <param name="ip">The IP address as a string.</param>
        /// <param name="port">The port number.</param>
        /// <exception cref="FormatException">Thrown if the IP string is not a valid IP address.</exception>
        public Host(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new FormatException($"Invalid IP address: {ip}");

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(nameof(port), $"{nameof(port)}: {port}");

            // not an ip, keep as hostname and resolve later
            if (IPAddress.TryParse(ip, out var address) is false)
            {
                // looks like an ip (digits and dots, or ipv6) but is invalid
                if (Uri.CheckHostName(ip) != UriHostNameType.Dns || ip.Contains(":") || ip.Replace(".", "").All(char.IsDigit))
                    throw new FormatException($"Invalid IP address: {ip}");

                Name = ip;
                _port = port;
                return;
            }

            _endpoint = new IPEndPoint(address, port);
        }

        /// <summary>
        ///     Creates a new <see cref="Host" /> from an <see cref="EndPoint" />.
        /// </summary>
        /// <param name="endpoint">The endpoint to copy.</param>
        public Host(EndPoint endpoint)
        {
            var temp = endpoint as IPEndPoint ?? throw new ArgumentException("EndPoint must be IPEndPoint");
            _endpoint = new IPEndPoint(temp.Address, temp.Port);
        }

        /// <summary>
        ///     Creates a new <see cref="Host" /> from an <see cref="IPEndPoint" />.
        /// </summary>
        /// <param name="ipEndpoint">The IPEndPoint to copy.</param>
        public Host(IPEndPoint ipEndpoint)
        {
            _endpoint = new IPEndPoint(ipEndpoint.Address, ipEndpoint.Port);
        }

        /// <summary>
        ///     Creates a new <see cref="Host" /> from an <see cref="IPAddress" /> and port.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="port">The port number.</param>
        public Host(IPAddress address, int port)
        {
            _endpoint = new IPEndPoint(address, port);
        }

        private readonly int _port;
        private IPEndPoint _endpoint;

        /// <summary>
        ///     Returns the hostname when the host was created from a DNS name, otherwise null.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Returns true when the host has an IP address (always true for hosts created from an IP).
        /// </summary>
        public bool IsResolved => _endpoint != null;

        /// <summary>
        ///     Resolves the hostname to an IP address (first IPv4, then IPv6). Does nothing if already resolved.
        /// </summary>
        /// <exception cref="SocketException">Thrown if the hostname cannot be resolved.</exception>
        public void Resolve()
        {
            if (IsResolved)
                return;

            var list = Dns.GetHostAddresses(Name);
            var address = Array.Find(list, x => x.AddressFamily == AddressFamily.InterNetwork)
                          ?? Array.Find(list, x => x.AddressFamily == AddressFamily.InterNetworkV6);

            if (address == null)
                throw new SocketException((int)SocketError.HostNotFound);

            _endpoint = new IPEndPoint(address, _port);
        }

        /// <summary>
        ///     Returns the <see cref="IPAddress" /> of the host.
        /// </summary>
        public IPAddress Address => IPEndPoint.Address;

        /// <summary>
        ///     Returns the port number of the host.
        /// </summary>
        public int Port => _endpoint?.Port ?? _port;

        /// <summary>
        ///     Returns the host as an <see cref="EndPoint" />.
        /// </summary>
        public EndPoint EndPoint => IPEndPoint;

        /// <summary>
        ///     Returns the <see cref="IPEndPoint" /> representing this host.
        /// </summary>
        public IPEndPoint IPEndPoint
        {
            get
            {
                Resolve();
                return _endpoint;
            }
        }

        /// <summary>
        ///     Returns the address family of the host.
        /// </summary>
        public AddressFamily AddressFamily => IPEndPoint.AddressFamily;

        /// <summary>
        ///     Returns a string representation of the host as "IP:Port".
        /// </summary>
        public override string ToString()
        {
            return IsResolved ? _endpoint.ToString() : $"{Name}:{_port}";
        }

        /// <summary>
        ///     Determines whether the specified object is equal to the current host by comparing IP and port.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object obj)
        {
            if (obj is Host other is false || Port != other.Port)
                return false;

            if (IsResolved && other.IsResolved)
                return Address.Equals(other.Address);

            return Name != null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Returns a hash code for this host.
        /// </summary>
        public override int GetHashCode()
        {
            return IsResolved ? _endpoint.GetHashCode() : (Name.ToLowerInvariant().GetHashCode() * 397) ^ _port;
        }

        /// <summary>
        ///     Implicitly converts a valid IP:Port string to an <see cref="Host" />.
        /// </summary>
        /// <param name="host">The host string (e.g., "127.0.0.1:3300").</param>
        /// <exception cref="FormatException">Thrown if the string is not a valid IP:Port format.</exception>
        public static implicit operator Host(string host)
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

            if (uri.Port < 0)
                throw new FormatException($"Invalid host format: {host}");

            // ipv6 uri host comes inside brackets
            return new Host(uri.Host.Trim('[', ']'), uri.Port);
        }
    }
}