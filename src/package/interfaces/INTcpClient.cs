using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Netly.Packages.Interfaces
{
    internal interface INTcpClient
    {
        string Id { get; }

        bool IsConnected { get; }
        bool IsSecure { get; }
        bool IsFraming { get; }
        bool IsServer { get; }
        SslProtocols SecureProtocol { get; }
        string SecureDomain { get; }

        long FramingSize { get; set; }

        Socket Socket { get; }
        Stream Stream { get; }
        Host Host { get; }
        X509Certificate Certificate { get; }

        void OnCreate(Action<Socket> callback);

        void OnStream(Func<long, Stream> callback);
        void OnConnect(Action callback);
        void OnFail(Action<Exception> callback);
        void OnEvent(Action<string, Stream> callback);
        void OnMessage(Action<Stream> callback);
        void OnDisconnect(Action callback);
        void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback);

        void ToMessage(byte[] message);
        void ToEvent(string name, byte[] message);
        void ToConnect(Host host);
        Task ToConnectAsync(Host host);
        void ToDisconnect();
        Task ToDisconnectAsync();

        void ToSecure(bool allowSecure, byte[] certificate = null, string password = null,
            SslProtocols protocols = SslProtocols.Default, string secureDomain = null);
    }
}