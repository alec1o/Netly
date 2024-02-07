using System;
using Netly.Core;

namespace Netly
{
    internal interface IHttpServer
    {
        bool IsOpen { get; }
        Uri Host { get; }

        void Open(Uri host);
        void OnOpen(Action callback);

        void OnError(Action<Exception> exception);

        void Close();
        void OnClose(Action callback);

        void MapAll(string path, Action<Request, Response> callback);
        void MapGet(string path, Action<Request, Response> callback);
        void MapPut(string path, Action<Request, Response> callback);
        void MapHead(string path, Action<Request, Response> callback);
        void MapPost(string path, Action<Request, Response> callback);
        void MapPatch(string path, Action<Request, Response> callback);
        void MapTrace(string path, Action<Request, Response> callback);
        void MapDelete(string path, Action<Request, Response> callback);
        void MapOptions(string path, Action<Request, Response> callback);

        void MapWebSocket(string path, Action<Request, WebSocketClient> callback);
    }
}