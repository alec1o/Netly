using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        internal interface IServer
        {
            string Id { get; }
            Host Host { get; }
            bool IsOpened { get; }
            bool IsFraming { get; }
            X509Certificate Certificate { get; }
            SslProtocols EncryptionProtocol { get; }
            bool IsEncrypted { get; }
            Server.ITo To { get; }
            Server.IOn On { get; }
            IClient[] Clients { get; }
        }
    }
}