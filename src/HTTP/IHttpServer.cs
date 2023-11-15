using System;
using Netly.Core;

namespace Netly
{
    internal interface IHttpServer
    {
        bool IsOpen { get; }
        Host Host { get; }
        
        void Open(Host host);
        void OnOpen(Action callback);

        void OnError(Action<Exception> exception);
        
        void Close();
        void OnClose(Action callback);

        void On(string path, Action<Request, Response> callback);
        void OnWebsocket(string path, Action<Request, WebSocketClient> callback);
    }
}