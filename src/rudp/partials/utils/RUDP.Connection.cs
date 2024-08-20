using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Connection
        {
            public readonly string Id;
            public readonly bool IsServer;
            public readonly Channel MyChannel;
            public readonly Host MyHost;
            public readonly Socket MySocket;


            public Connection(Host host, Socket socket, bool isServer, string id)
            {
                Id = id;
                MyHost = host;
                MySocket = socket;
                IsServer = isServer;
                MyChannel = new Channel(host)
                {
                    SendRaw = SendRaw,
                    OnRawData = OnRawDataHandler
                };
            }

            public bool IsOpened { get; }
            public bool IsConnecting { get; private set; }
            public Action OnOpen { private get; set; }
            public Action OnClose { private get; set; }
            public Action<string> OnOpenFail { private get; set; }
            public Action<byte[], MessageType> OnData { private get; set; }
            public Action<string, byte[], MessageType> OnEvent { private get; set; }
            public int HandshakeTimeout { get; set; }
            public int NoResponseTimeout { get; set; }
            public Action<bool> OnServer { get; set; }

            private void OnRawDataHandler(byte[] data, MessageType messageType)
            {
                throw new NotImplementedException();
            }

            private void SendRaw(byte[] bytes)
            {
                if (IsServer)
                    MySocket.BeginSendTo(bytes, 0, bytes.Length, SocketFlags.None, MyHost.EndPoint, null, null);
                else
                    MySocket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, null, null);
            }

            public void Send(ref byte[] data, MessageType messageType)
            {
                MyChannel.ToAddData(data, messageType);
            }

            public Task Open()
            {
                IsConnecting = true;

                _ = Task.Run(() =>
                {
                    if (IsOpened || IsConnecting)
                    {
                        MyChannel.ToUpdateReliableQueue();
                        const float value = Channel.ResentTimeout;
                        const float timeoutResult = value / 4F;
                        const int timeout = (int)timeoutResult;
                        Thread.Sleep(timeout);
                    }
                });

                throw new NotImplementedException();
            }

            public Task Close()
            {
                throw new NotImplementedException();
            }

            public void InjectBuffer(byte[] bytes)
            {
                MyChannel.OnReceiveRaw(ref bytes, MyHost);
            }
        }
    }
}