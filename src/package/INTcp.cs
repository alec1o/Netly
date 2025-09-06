using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Netly.Packages
{
    interface INTcp
    {
        string Id { get; }

        bool IsConnected { get; }
        bool IsSecure { get; }
        bool IsFraming { get; }
        bool IsServer { get; }

        long MaxFrameSize { get; set; }

        Socket Socket { get; }
        Stream Stream { get; }
        Host Host { get; }
        X509Certificate Certificate { get; }

        void OnCreate(Action<Socket> callback);

        void OnStream(Func<long, Stream> callback);
        void OnConnect(Action callback);
        void OnFail(Action<Exception> callback);
        void OnEvent(Action<string, byte[]> callback);
        void OnMessage(Action<byte[]> callback);
        void OnDisconnect(Action callback);
        void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback);

        void ToMessage(byte[] message);
        void ToEvent(string name, byte[] message);
        void ToConnect(Host host);
        Task ToConnectAsync(Host host);
        void ToDisconnect();
        void ToSecure(bool enableSecureMode, X509Certificate certificate = null);
        Task ToDisconnectAsync();
    }
}