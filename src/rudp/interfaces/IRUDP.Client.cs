namespace Netly.Interfaces
{
    public static partial class IRUDP
    {
        /// <summary>
        ///     RUDP Client instance (interface)
        /// </summary>
        public interface Client
        {
            /// <summary>
            ///     Client ID (readonly)
            /// </summary>
            string Id { get; }

            /// <summary>
            ///     Remote Host
            /// </summary>
            Host Host { get; }

            /// <summary>
            ///     Is Opened? (return true if connected)
            /// </summary>
            bool IsOpened { get; }

            /// <summary>
            ///     Connection handshake timeout. [is milliseconds >= 1000]
            /// <br/>NOTE: (default value is 5000ms or 5s; 
            /// </summary>
            int HandshakeTimeout { get; set; }

            /// <summary>
            ///     Timeout that a connection can remain unresponsive, after the timeout the connection will be closed. [is milliseconds >= 2000]
            /// <br/>NOTE: (default value is 5000ms or 5s)
            /// <br/>NOTE: Works after handshake successful
            /// </summary>
            int NoResponseTimeout { get; set; }

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