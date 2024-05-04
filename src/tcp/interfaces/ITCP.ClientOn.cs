using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface ClientOn : IOn<Socket>
        {
            void Data(Action<byte[]> callback);
            void Event(Action<string, byte[]> callback);
            void Encryption(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback);
        }
    }
}