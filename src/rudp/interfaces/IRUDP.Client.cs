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
            ///     Connection opening timeout. is Milliseconds
            /// <br/>(default value is 5000ms or 5s)
            /// </summary>
            int OpenTimeout { get; set; }

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