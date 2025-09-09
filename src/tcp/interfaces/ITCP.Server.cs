using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        internal interface Server
        {
            string Id { get; }
            NHost Host { get; }
            bool IsOpened { get; }
            bool IsFraming { get; }
            Socket Socket { get; }
            IFraming Framing { get; }
            X509Certificate Certificate { get; }
            SslProtocols EncryptionProtocol { get; }
            bool IsEncrypted { get; }
            ServerTo To { get; }
            ServerOn On { get; }
            List<Client> Clients { get; }
        }
    }
}