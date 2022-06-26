using System;
using System.Net.Sockets;

namespace Zenet.Core
{
    public interface IClient
    {
        Socket Socket { get; }
        ZHost Host { get; }
        bool Opened { get; }
        string Id { get; }

        // Controller
        void Open(ZHost host);
        void Close();
        void ToData(byte[] data);
        void ToEvent(string name, byte[] data);

        // Events
        void OnOpen(Action callback);
        void OnError(Action<Exception> callback);
        void OnClose(Action callback);
        void OnData(Action<byte[]> callback);
        void OnEvent(Action<string, byte[]> callback);
        void BeforeOpen(Action<Socket> callback);
        void AfterOpen(Action<Socket> callback);
    }
}
