using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Byter;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Connection
        {
            private const int
                HandshakeDataPrefix = -1024 * (int)Math.PI * 2, //# -6144
                HandshakeData1 /**/ = -2048 * (int)Math.PI * 4, //# -24576
                HandshakeData2 /**/ = -4096 * (int)Math.PI * 8; //# -98304

            public readonly List<int> HandshakeDataQueue = new List<int>();
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

            public bool IsOpened { get; private set; }
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
                var primitive = new Primitive(data);

                var prefix = primitive.Get.Int();
                var content = primitive.Get.Int();

                if (primitive.IsValid)
                    if (prefix == HandshakeDataPrefix && IsConnecting)
                        HandshakeDataQueue.Add(content);
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
                IsOpened = false;
                IsConnecting = true;
                HandshakeDataQueue.Clear();

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

                var connect = new Task(() =>
                {
                    var timeoutAt = DateTime.UtcNow.AddMilliseconds(HandshakeTimeout);
                    var success = false;
                    var failMessage = $"Connection closed, Handshake Timeout: {HandshakeTimeout}ms";

                    if (!IsServer)
                    {
                        var primitive = new Primitive();

                        // send 1st handshake data
                        {
                            primitive.Add.Int(HandshakeDataPrefix);
                            primitive.Add.Int(HandshakeData1);

                            var data = primitive.GetBytes();

                            Send(ref data, MessageType.Reliable);
                        }

                        primitive.Reset();

                        // send 2nd handshake data
                        {
                            primitive.Add.Int(HandshakeDataPrefix);
                            primitive.Add.Int(HandshakeData2);

                            var data = primitive.GetBytes();

                            Send(ref data, MessageType.Reliable);
                        }
                    }

                    while (timeoutAt > DateTime.UtcNow)
                        if (HandshakeDataQueue.Count == 2)
                        {
                            if (HandshakeDataQueue[0] == HandshakeData1 && HandshakeDataQueue[1] == HandshakeData2)
                                success = true;

                            failMessage = "Invalid Handshake Method";
                            break;
                        }

                    if (success)
                    {
                        IsOpened = true;
                        IsConnecting = false;
                        OnOpen();
                    }
                    else
                    {
                        IsConnecting = false;
                        IsOpened = false;

                        OnOpenFail(failMessage);
                    }
                });

                return Task.Run(() => connect.Start());
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