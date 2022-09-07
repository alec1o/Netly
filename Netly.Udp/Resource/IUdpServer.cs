using Netly.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Netly.Udp
{
    internal interface IUdpServer
    {
        bool Opened { get; }
        int Timeout { get; }

        void Open(Host host, int backlog = 0);
        void Close();
        void UseTimeout(int value);

        void OnOpen(Action callback);
        void OnClose(Action callback);
        void OnError(Action<Exception> callback);
        void OnEnter(Action<UdpClient> callback);
        void OnExit(Action<UdpClient> callback);
        void OnData(Action<UdpClient, byte[]> callback);
        void OnEvent(Action<UdpClient, string, byte[]> callback);

        void OnBeforeOpen(Action<Socket> callback);
        void OnAfterOpen(Action<Socket> callback);
    }
}
