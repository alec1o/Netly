using System;
using System.Net;
using System.Net.WebSockets;
using Netly.Core;

namespace Netly
{
    internal interface IWebsocketClient
    {
        bool IsOpened { get; }
        Uri Uri { get; }
        KeyValueContainer Headers { get; }
        Cookie[] Cookies { get; }

        void Open(Uri uri);
        void Close();
        void Close(WebSocketCloseStatus status);

        void ToData(byte[] buffer, BufferType bufferType);
        void ToData(string buffer, BufferType bufferType);

        void ToEvent(string name, byte[] buffer);
        void ToEvent(string name, string buffer);

        void OnOpen(Action callback);
        void OnClose(Action callback);
        void OnClose(Action<WebSocketCloseStatus> callback);
        void OnError(Action<Exception> callback);
        void OnData(Action<byte[], BufferType> callback);
        void OnEvent(Action<string, byte[], BufferType> callback);
        void OnModify(Action<ClientWebSocket> callback);
    }
}
