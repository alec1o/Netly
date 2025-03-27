using System;
using System.Net.Sockets;

namespace Netly.Interfaces
{
    public static partial class IRUDP
    {
        /// <summary>
        ///     RUDP Server callbacks container (interface)
        /// </summary>
        public interface ServerOn : IOn<Socket>
        {
            /// <summary>
            ///     Use to handle new client onboarding
            /// </summary>
            /// <param name="callback">Callback function</param>
            void Accept(Action<Client> callback);
        }
    }
}