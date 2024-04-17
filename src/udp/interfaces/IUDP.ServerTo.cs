using System.Threading.Tasks;
using Netly.Core;

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
            Task Open(Host host);

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
            /// <param name="encoding">Data encoding method</param>
            void DataBroadcast(string data, NE.Encoding encoding = NE.Encoding.UTF8);

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
            /// <param name="encoding">Event data encoding method</param>
            void EventBroadcast(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8);

            /// <summary>
            ///     Use to send raw data to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="data">data - bytes </param>
            void Data(Host targetHost, byte[] data);

            /// <summary>
            ///     Use to send raw data to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            void Data(Host targetHost, string data, NE.Encoding encoding = NE.Encoding.UTF8);

            /// <summary>
            ///     Use to send event (netly event) to a custom host
            /// </summary>
            /// <param name="host">Target host</param>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            void Event(Host host, string name, byte[] data);

            /// <summary>
            ///     Use to send event (netly event) to a custom host
            /// </summary>
            /// <param name="targetHost">Target host</param>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            void Event(Host targetHost, string name, string data, NE.Encoding encoding = NE.Encoding.UTF8);
        }
    }
}