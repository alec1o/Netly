using Netly.Core;
using System;
using System.Net.Sockets;

namespace Netly
{
    internal interface IUdpServer
    {
        Host Host { get; }
        int Timeout { get; }
        bool IsOpened { get; }
        bool UseConnection { get; }
        UdpClient[] Clients { get; }

        void Open(Host host);
        void Close();
        void ToData(byte[] buffer);
        void ToData(string buffer);
        void ToEvent(string name, byte[] buffer);
        void ToEvent(string name, string buffer);

        void OnOpen(Action callback);
        void OnError(Action<Exception> callback);
        void OnClose(Action callback);
        void OnData(Action<UdpClient, byte[]> callback);
        void OnEvent(Action<UdpClient, string, byte[]> callback);
        void OnEnter(Action<UdpClient> callback);
        void OnExit(Action<UdpClient> callback);
        void OnModify(Action<Socket> callback);
    }
}
