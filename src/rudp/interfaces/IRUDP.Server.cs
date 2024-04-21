using Netly.Core;

namespace Netly.Interfaces
{
    public static partial class IRUDP
    {
        /// <summary>
        ///     RUDP Server (interface)
        /// </summary>
        public interface Server
        {
            /// <summary>
            ///     Server ID (readonly)
            /// </summary>
            string Id { get; }

            /// <summary>
            ///     Server host (bind endpoint)
            /// </summary>
            Host Host { get; }

            /// <summary>
            ///     Is Opened? (true if is bind)
            /// </summary>
            bool IsOpened { get; }

            /// <summary>
            ///     Actions container
            /// </summary>
            ServerTo To { get; }

            /// <summary>
            ///     Callbacks container
            /// </summary>
            ServerOn On { get; }

            /// <summary>
            ///     Collections of connected client
            /// </summary>
            Client[] Clients { get; }
        }
    }
}