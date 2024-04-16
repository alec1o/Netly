using System.Threading.Tasks;
using Netly.Core;

namespace Netly.Interfaces
{
    public static partial class IUDP
    {
        /// <summary>
        ///     UDP-Server actions container (interface)
        /// </summary>
        public interface ServerTo
        {
            /// <summary>
            ///     Use to open connection (if disconnected)
            /// </summary>
            /// <param name="host">Host (local endpoint)</param>
            /// <returns></returns>
            Task Open(Host host);

            /// <summary>
            ///     Use to close connection (if connected)
            /// </summary>
            /// <returns></returns>
            Task Close();
        }
    }
}