using System.Security.Authentication;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        public partial class Server
        {
            public interface ITo
            {
                Task Open(Host host);
                Task Open(Host host, int backlog);
                Task Close();

                void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword,
                    SslProtocols protocols);
            }
        }
    }
}