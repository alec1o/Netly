using Netly.Core;
using System;
using System.Net.Sockets;

namespace Netly
{
    internal interface IServer<T>
    {
        Host Host { get; }
        bool IsOpened { get; }

        void Open(Host host);
        void OnError(Action<Exception> callback);
        void OnOpen(Action callback);

        void Close();
        void OnClose(Action callback);

        void OnEnter(Action<T> callback);
        void OnExit(Action<T> callback);

        void OnData(Action<T, byte[]> callback);
        void OnEvent(Action<T, string, byte[]> callback);

        void OnModify(Action<Socket> callback);
    }
}