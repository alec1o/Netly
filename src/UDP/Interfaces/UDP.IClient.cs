using Netly.Core;

namespace Netly.Interfaces
{
    public static partial class IUDP
    {
        /// <summary>
        ///     Netly UDP-Client (interface)
        /// </summary>
        public interface Client
        {
            /// <summary>
            ///     Client ID (readonly)
            /// </summary>
            string Id { get; }

            /// <summary>
            ///     Client EndPoint
            /// </summary>
            Host Host { get; }

            /// <summary>
            ///     Is Opened? (return true if connected)
            /// </summary>
            bool IsOpened { get; }

            /// <summary>
            ///     Actions container
            /// </summary>
            ClientTo To { get; }

            /// <summary>
            ///     Callbacks container
            /// </summary>
            ClientOn On { get; }
        }
    }
}