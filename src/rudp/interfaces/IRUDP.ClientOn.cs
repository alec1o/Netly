using System;
using System.Net.Sockets;

namespace Netly.Interfaces
{
    public static partial class IRUDP
    {
        /// <summary>
        ///     RUDP Client callbacks container (interface)
        /// </summary>
        public interface ClientOn : IOn<Socket>
        {
            /// <summary>
            ///     Use to handle raw data receiving event
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Data(Action<byte[], RUDP.MessageType> callback);

            /// <summary>
            ///     Use to handle event receive event (netly event)
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Event(Action<string, byte[], RUDP.MessageType> callback);
        }
    }
}