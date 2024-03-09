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
                void Open(Host host);
                void Open(Host host, int backlog);
                void Close();
                void Encryption(bool enable, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols);
            }
        }
    }
}