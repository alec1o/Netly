using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        /// <summary>
        /// UDP-Client actions container (interface)
        /// </summary>
        public interface IClientTo
        {
            /// <summary>
            /// Use to open connection (if disconnected)
            /// </summary>
            /// <param name="host">Host (remote endpoint)</param>
            /// <returns></returns>
            Task Open(Host host);
            
            /// <summary>
            /// Use to close connection (if connected)
            /// </summary>
            /// <returns></returns>
            Task Close();
            
            /// <summary>
            /// Send raw data
            /// </summary>
            /// <param name="data">data - bytes</param>
            void Data(byte[] data);
            
            /// <summary>
            /// Send raw data
            /// </summary>
            /// <param name="data">Data - string</param>
            /// <param name="encoding">Data encoding method</param>
            void Data(string data, NE.Encoding encoding = NE.Encoding.UTF8);
            
            /// <summary>
            /// Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - bytes</param>
            void Event(string name, byte[] data);
            
            /// <summary>
            /// Send event (netly event)
            /// </summary>
            /// <param name="name">Event name</param>
            /// <param name="data">Event data - string</param>
            /// <param name="encoding">Event data encoding method</param>
            void Event(string name, string data, NE.Encoding encoding = NE.Encoding.UTF8);
        }
    }
}