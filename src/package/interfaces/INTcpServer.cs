using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Netly.Packages.Interfaces
{
    internal interface INTcpServer
    {
        string Id { get; }

        bool IsConnected { get; }
        bool IsSecure { get; }
        bool IsFraming { get; }
        bool AllowUnsecured { get; }
        long FramingSize { get; set; }
        IList<NTcpClient> Clients { get; }

        Socket Socket { get; }
        Host Host { get; }
        X509Certificate Certificate { get; }
        SslProtocols SecureProtocol { get; }

        void OnCreate(Action<Socket> callback);

        void OnStream(Func<long, Stream> callback);
        void OnConnect(Action callback);
        void OnFail(Action<Exception> callback);
        void OnAccept(Action<NTcpClient> callback);
        void OnDisconnect(Action callback);
        void OnSecure(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback);

        void ToBroadcastMessage(byte[] message, Func<NTcpClient, bool> condition = null);
        void ToBroadcastEvent(string name, byte[] message, Func<NTcpClient, bool> condition = null);
        void ToConnect(Host host);
        Task ToConnectAsync(Host host);
        void ToConnect(Host host, int backlog);
        Task ToConnectAsync(Host host, int backlog);
        void ToDisconnect();

        void ToSecure(byte[] certificate, string password, SslProtocols protocols, bool allowUnsecured);

        Task ToDisconnectAsync();
    }
}