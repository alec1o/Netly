using System;
using System.Net.Sockets;

namespace Netly.Interfaces
{
    public static partial class IUDP
    {
        /// <summary>
        ///     UDP-Server callbacks container (interface)
        /// </summary>
        public interface ServerOn : IOn<Socket>
        {
            /// <summary>
            ///     Use to handle accepted client
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Accept(Action<Client> callback);
        }
    }
}