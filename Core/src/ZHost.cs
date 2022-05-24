using System;
using System.Net;
using System.Net.Sockets;

namespace Zenet.Core
{
    public class ZHost
    {
        public readonly IPEndPoint EndPoint;
        public readonly int Port;
        public readonly IPAddress IPAddress;
        public readonly AddressFamily Family;        

        public ZHost(EndPoint value)
        {
            EndPoint = (IPEndPoint)value;
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public ZHost(IPEndPoint value)
        {
            EndPoint = new IPEndPoint(value.Address, value.Port);
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public ZHost(string ip, int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public ZHost(IPAddress address, int port)
        {
            EndPoint = new IPEndPoint(address, port);
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public override string ToString()
        {
            return EndPoint.ToString();
        }
    }
}
