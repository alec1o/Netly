using System.Security.Authentication;
using System.Threading.Tasks;

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
            void DataBroadcast(byte[] data);
            void EventBroadcast(string name, string data);
            void EventBroadcast(string name, byte[] data);
            void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols);
        }
    }
}