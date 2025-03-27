using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        internal interface Server
        {
            string Id { get; }
            Host Host { get; }
            bool IsOpened { get; }
            bool IsFraming { get; }
            X509Certificate Certificate { get; }
            SslProtocols EncryptionProtocol { get; }
            bool IsEncrypted { get; }
            ServerTo To { get; }
            ServerOn On { get; }
            Client[] Clients { get; }
        }
    }
}