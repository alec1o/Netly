using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface ClientOn : IOn<Socket>
        {
            void Data(Action<Stream> callback);
            void Event(Action<string, Stream> callback);
            void Encryption(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback);
        }
    }
}