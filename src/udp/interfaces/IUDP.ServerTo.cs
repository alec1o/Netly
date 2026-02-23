using System.Text;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class IUDP
    {
        /// <summary>
        ///     UDP Server actions container (interface)
        /// </summary>
        public interface ServerTo
        {
            /// <summary>
            ///     Use to open listening connection
            /// </summary>
            /// <param name="host">Host (local endpoint)</param>
            /// <returns></returns>
            Task Open(NHost host);

            /// <summary>
            ///     Use to close listening connection
            /// </summary>
            /// <returns></returns>
            Task Close();

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

            /// <summary>
            ///     Use to send raw data to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="data">data - bytes </param>
            void Data(NHost targetHost, byte[] data);

            /// <summary>
            ///     Use to send raw data to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="data">Data - string</param>
            void Data(NHost targetHost, string data);

            /// <summary>
            ///     Use to send raw data to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            void Data(NHost targetHost, string data, Encoding encoding);

            /// <summary>
            ///     Use to send event (netly event) to a custom host
            /// </summary>
            /// <param name="host">Target host</param>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            void Event(NHost host, string name, byte[] data);

            /// <summary>
            ///     Use to send event (netly event) to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            void Event(NHost targetHost, string name, string data);

            /// <summary>
            ///     Use to send event (netly event) to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            void Event(NHost targetHost, string name, string data, Encoding encoding);
        }
    }
}