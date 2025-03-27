using System.Text;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class IRUDP
    {
        /// <summary>
        ///     RUDP Client actions container (interface)
        /// </summary>
        public interface ClientTo
        {
            /// <summary>
            ///     Use to open connection (if disconnected)
            /// </summary>
            /// <param name="host">Host (remote endpoint)</param>
            /// <returns></returns>
            Task Open(Host host);

            /// <summary>
            ///     Use to close connection (if connected)
            /// </summary>
            /// <returns></returns>
            Task Close();

            /// <summary>
            ///     Send raw data
            /// </summary>
            /// <param name="data">data - bytes</param>
            /// <param name="messageType">message type</param>
            void Data(byte[] data, RUDP.MessageType messageType);

            /// <summary>
            ///     Send raw data
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="messageType">message type</param>
            void Data(string data, RUDP.MessageType messageType);

            /// <summary>
            ///     Send raw data
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            /// <param name="messageType">message type</param>
            void Data(string data, RUDP.MessageType messageType, Encoding encoding);


            /// <summary>
            ///     Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            /// <param name="messageType">message type</param>
            void Event(string name, byte[] data, RUDP.MessageType messageType);

            /// <summary>
            ///     Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="messageType">message type</param>
            void Event(string name, string data, RUDP.MessageType messageType);

            /// <summary>
            ///     Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            /// <param name="messageType">message type</param>
            void Event(string name, string data, RUDP.MessageType messageType, Encoding encoding);
        }
    }
}