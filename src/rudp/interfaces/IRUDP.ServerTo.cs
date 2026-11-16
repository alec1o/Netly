using System.Text;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class IRUDP
    {
        /// <summary>
        ///     RUDP Server actions container (interface)
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
            /// <param name="messageType">message type</param>
            void DataBroadcast(byte[] data, RUDP.MessageType messageType);

            /// <summary>
            ///     Use to send raw data to all connected clients
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="messageType">message type</param>
            void DataBroadcast(string data, RUDP.MessageType messageType);

            /// <summary>
            ///     Use to send raw data to all connected clients
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            /// <param name="messageType">message type</param>
            void DataBroadcast(string data, RUDP.MessageType messageType, Encoding encoding);

            /// <summary>
            ///     Use to send event (netly event) to all connected clients
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            /// <param name="messageType">message type</param>
            void EventBroadcast(string name, byte[] data, RUDP.MessageType messageType);

            /// <summary>
            ///     Use to send event (netly event) to all connected clients
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="messageType">message type</param>
            void EventBroadcast(string name, string data, RUDP.MessageType messageType);

            /// <summary>
            ///     Use to send event (netly event) to all connected clients
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            /// <param name="messageType">message type</param>
            void EventBroadcast(string name, string data, RUDP.MessageType messageType, Encoding encoding);
        }
    }
}