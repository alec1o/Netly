using System;
using System.Net;
using System.Net.Sockets;

namespace Zenet.Network
{
    public class Host
    {
        public readonly IPEndPoint EndPoint;
        public readonly int Port;
        public readonly IPAddress IPAddress;
        public readonly AddressFamily Family;        

        public Host(EndPoint value)
        {
            EndPoint = (IPEndPoint)value;
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public Host(IPEndPoint value)
        {
            EndPoint = new IPEndPoint(value.Address, value.Port);
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public Host(string ip, int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }

        public Host(IPAddress address, int port)
        {
            EndPoint = new IPEndPoint(address, port);
            Port = EndPoint.Port;
            IPAddress = EndPoint.Address;
            Family = EndPoint.AddressFamily;
        }
    }
}
