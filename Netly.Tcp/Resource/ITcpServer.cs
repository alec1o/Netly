using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Netly.Tcp
{
    internal interface ITcpServer
    {
        bool Opened { get; }
        bool IsEncrypted { get; }

        void Open(Host host, int backlog = 0);
        void Close();
        void UseEncryption(bool value);

        void OnOpen(Action callback);
        void OnClose(Action callback);
        void OnError(Action<Exception> callback);
        void OnEnter(Action<TcpClient> callback);
        void OnExit(Action<TcpClient> callback);
        void OnData(Action<TcpClient, byte[]> callback);
        void OnEvent(Action<TcpClient, string, byte[]> callback);

        void OnBeforeOpen(Action<Socket> callback);
        void OnAfterOpen(Action<Socket> callback);
    }
}
