using System;
using System.Net.WebSockets;

namespace Netly.Interfaces
{
    public interface IOnWebSocket : IOn<ClientWebSocket>
    {
        /// <summary>
        /// Handle data received
        /// </summary>
        /// <param name="callback">Callback</param>
        void Data(Action<byte[], bool> callback);

        /// <summary>
        /// Handle (netly event) received
        /// </summary>
        /// <param name="callback">Callback</param>
        void Event(Action<string, byte[]> callback);

        /// <summary>
        /// Handle connection closed
        /// </summary>
        /// <param name="callback">Callback</param>
        void Close(Action<WebSocketCloseStatus> callback);
    }
}