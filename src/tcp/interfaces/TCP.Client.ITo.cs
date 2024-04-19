using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Client
        {
            public interface ITo
            {
                Task Open(Host host);
                Task Close();
                void Data(byte[] data);
                void Encryption(bool enable);
                void Data(string data);
                void Data(string data, Encoding encoding);
                void Event(string name, byte[] data);
                void Event(string name, string data);
                void Event(string name, string data, Encoding encoding);
            }
        }
    }
}