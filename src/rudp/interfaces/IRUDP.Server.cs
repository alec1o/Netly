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
            ///     Server connection handshake timeout. [is milliseconds >= 1000]
            /// <br/>NOTE: (default value is 5000ms or 5s; 
            /// </summary>
            int HandshakeTimeout { get; set; }
            
            /// <summary>
            ///     Timeout that a client connection can remain unresponsive, after the timeout the connection will be closed. [is milliseconds >= 2000]
            /// <br/>NOTE: (default value is 5000ms or 5s)
            /// <br/>NOTE: Works after handshake successful
            /// </summary>
            int NoResponseTimeout { get; set; }

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