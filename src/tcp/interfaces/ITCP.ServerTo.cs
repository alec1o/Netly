using System.Security.Authentication;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface ServerTo
        {
            Task Open(Host host);
            Task Open(Host host, int backlog);
            Task Close();
            void DataBroadcast(string data);
            void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols);
        }
    }
}