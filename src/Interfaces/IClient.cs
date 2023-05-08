using Netly.Core;
using System;
using System.Net.Sockets;
using UdpClient = Netly.UdpClient;
namespace Netly
{
    internal interface IClient
    {
        Host Host { get; }
        bool IsOpened { get; }

        void Open(Host host);
        void OnError(Action<Exception> callback);
        void OnOpen(Action callback);

        void Close();
        void OnClose(Action callback);

        void ToData(byte[] data);
        void ToData(string data);
        void OnData(Action<byte[]> callback);

        void ToEvent(string name, byte[] data);
        void ToEvent(string name, string data);
        void OnEvent(Action<string, byte[]> callback);

        void OnModify(Action<Socket> callback);
    }
}
