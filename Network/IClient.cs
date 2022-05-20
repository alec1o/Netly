using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Zenet.Network
{
    public interface IClient
    {
        Socket Socket { get; }
        Host Host { get; }
        bool Opened { get; }

        // Controller
        void Open(Host host);
        void Close();
        void ToData(byte[] data);
        void ToEvent(string name, byte[] data);

        // Events
        void OnOpen(Action callback);
        void OnError(Action<Exception> callback);
        void OnClose(Action callback);
        void OnData(Action<byte[]> callback);
        void OnEvent(Action<string, byte[]> callback);
    }
}
