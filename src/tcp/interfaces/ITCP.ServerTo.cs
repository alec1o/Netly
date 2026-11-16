using System.Security.Authentication;
using System.Text;
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
            void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols);

            /// <summary>
            ///     Enable SSL/TLS on server
            /// </summary>
            /// <param name="enableEncryption">Enable or disable encryption</param>
            /// <param name="pfxCertificate">Server certificate (pfx)</param>
            /// <param name="pfxPassword">Server certificate password</param>
            /// <param name="protocols">Allowed SSL/TLS protocols</param>
            /// <param name="clientCertificateRequired">Reject clients that do not present a valid certificate</param>
            void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols,
                bool clientCertificateRequired);
            
            /// <summary>
            ///     Use to send raw data to all connected clients
            /// </summary>
            /// <param name="data">data - bytes</param>
            void DataBroadcast(byte[] data);

            /// <summary>
            ///     Use to send raw data to all connected clients
            /// </summary>
            /// <param name="data">Data - string</param>
            void DataBroadcast(string data);

            /// <summary>
            ///     Use to send raw data to all connected clients
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            void DataBroadcast(string data, Encoding encoding);

            /// <summary>
            ///     Use to send event (netly event) to all connected clients
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            void EventBroadcast(string name, byte[] data);

            /// <summary>
            ///     Use to send event (netly event) to all connected clients
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            void EventBroadcast(string name, string data);

            /// <summary>
            ///     Use to send event (netly event) to all connected clients
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            void EventBroadcast(string name, string data, Encoding encoding);

        }
    }
}