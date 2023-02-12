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

        void ToData(byte[] value);
        void OnData(Action<byte[]> callback);

        void ToEvent(string name, byte[] value);
        void OnEvent(Action<string, byte[]> callback);

        void OnModify(Action<Socket> callback);
    }
}
