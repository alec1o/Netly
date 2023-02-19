using Netly.Core;
using System;
using System.Net.Sockets;
using TcpClient = Netly.TcpClient;

namespace Netly
{
    internal interface ITcpServer
    {
        Host Host { get; }
        bool IsOpened { get; }

        void Open(Host host);
        void OnError(Action<Exception> callback);
        void OnOpen(Action callback);

        void Close();
        void OnClose(Action callback);

        void OnEnter(Action<TcpClient> callback);
        void OnExit(Action<TcpClient> callback);

        void OnData(Action<TcpClient, byte[]> callback);
        void OnEvent(Action<TcpClient, string, byte[]> callback);

        void OnModify(Action<Socket> callback);
    }
}
