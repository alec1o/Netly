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
                Task Encryption(bool enable, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols);
            }
        }
    }
}