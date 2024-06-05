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
            void Data(byte[] data);

            /// <summary>
            ///     Send raw data
            /// </summary>
            /// <param name="data">Data - string</param>
            void Data(string data);

            /// <summary>
            ///     Send raw data
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            void Data(string data, Encoding encoding);


            /// <summary>
            ///     Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            void Event(string name, byte[] data);

            /// <summary>
            ///     Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            void Event(string name, string data);

            /// <summary>
            ///     Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            void Event(string name, string data, Encoding encoding);
        }
    }
}