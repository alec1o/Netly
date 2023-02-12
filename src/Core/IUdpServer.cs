using System;
using System.Net.Sockets;
using TcpClient = Netly.TcpClient;

namespace Netly
{
    internal interface IUdpServer
    {
        Host Host { get; }
        bool IsOpened { get; }

        void Open(Host host);
        void OnError(Action<Exception> callback);
        void OnOpen(Action callback);

        void Close();
        void OnClose(Action callback);

        void OnEnter(Action<UdpClient> callback);
        void OnExit(Action<UdpClient> callback);

        void OnData(Action<UdpClient, byte[]> callback);
        void OnEvent(Action<UdpClient, string, byte[]> callback);

        void OnModify(Action<Socket> callback);
    }
}
