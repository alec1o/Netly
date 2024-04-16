using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        /// <summary>
        ///     Netly UDP-Server (interface)
        /// </summary>
        public interface IServer
        {
            /// <summary>
            ///     Server id (readonly)
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
            IServerTo To { get; }

            /// <summary>
            ///     Callbacks container
            /// </summary>
            IServerOn On { get; }

            /// <summary>
            ///     Collections of connected client
            /// </summary>
            IClient[] Clients { get; }
        }
    }
}