using Netly.Core;
using System;
using System.Net.Sockets;

namespace Netly.Udp
{
    internal interface IUdpClient
    {
        string Id { get; }
        bool Opened { get; }

        void Open(Host host);
        void Close();

        void ToData(byte[] value);
        void ToEvent(string name, byte[] value);

        void OnOpen(Action callback);
        void OnClose(Action callback);
        void OnError(Action<Exception> callback);
        void OnData(Action<byte[]> callback);
        void OnEvent(Action<string, byte[]> callback);

        void OnBeforeOpen(Action<Socket> callback);
        void OnAfterOpen(Action<Socket> callback);
    }
}
