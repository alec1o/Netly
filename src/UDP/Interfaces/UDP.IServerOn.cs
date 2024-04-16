using System;
using System.Net.Sockets;

namespace Netly
{
    public static partial class UDP
    {
        /// <summary>
        /// UDP-Server callbacks container (interface)
        /// </summary>
        public interface IServerOn : IOn<Socket>
        {
            /// <summary>
            /// Use to handle accepted client
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Accept(Action<IClient> callback);
        }
    }
}