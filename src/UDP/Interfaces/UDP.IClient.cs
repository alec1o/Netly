using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        /// <summary>
        /// Netly UDP-Client (interface)
        /// </summary>
        public interface IClient
        {
            /// <summary>
            /// Client ID (readonly)
            /// </summary>
            string Id { get; }
            
            /// <summary>
            /// Client EndPoint
            /// </summary>
            Host Host { get; }
            
            /// <summary>
            /// Is Opened? (return true if connected)
            /// </summary>
            bool IsOpened { get; }
            
            /// <summary>
            /// Actions container
            /// </summary>
            IClientTo To { get; }
            
            /// <summary>
            /// Callbacks container
            /// </summary>
            IClientOn On { get; }
        }
    }
}