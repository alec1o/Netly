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
                HandshakeData2 /**/ = -4096 * (int)Math.PI * 8, //# -98304
                HandshakeData3 /**/ = -8192 * (int)Math.PI * 16; //# -393216

            public readonly List<int> HandshakeDataQueue = new List<int>();
            public string Id = string.Empty;
            public readonly bool IsServer;
            public readonly Channel MyChannel;
            public readonly Host MyHost;
            public readonly Socket MySocket;


            public Connection(Host host, Socket socket, bool isServer)
            {
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
            public Action<bool> StartServerSideConnection { get; set; }
            private string _receivedClientId = String.Empty;

            private void OnRawDataHandler(byte[] data, MessageType messageType)
            {
                var primitive = new Primitive(data);

                var prefix = primitive.Get.Int();
                var content = primitive.Get.Int();


                if (primitive.IsValid)
                    if (prefix == HandshakeDataPrefix && IsConnecting)
                    {
                        if (IsServer)
                        {
                            if (content == HandshakeData1 || content == HandshakeData2)
                            {
                                HandshakeDataQueue.Add(content);
                            }
                        }
                        else
                        {
                            if (content == HandshakeData3)
                            {
                                var id = primitive.Get.String();

                                if (primitive.IsValid)
                                {
                                    HandshakeDataQueue.Add(content);
                                    _receivedClientId = id;
                                }
                            }
                        }
                    }

                Console.WriteLine(
                    $"+ Received Row: ({data.GetString()}), prefix: {prefix}, content: {content} type: {messageType}");
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
                _receivedClientId = string.Empty;

                _ = Task.Run(() =>
                {
                    while (IsOpened || IsConnecting)
                    {
                        MyChannel.ToUpdateReliableQueue();
                        Thread.Sleep(5);
                    }
                });

                var connect = new Task(() =>
                {
                    var timeoutAt = DateTime.UtcNow.AddMilliseconds(HandshakeTimeout);
                    var isConnected = false;
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
                    {
                        if (IsServer)
                        {
                            if (HandshakeDataQueue.Count == 2)
                            {
                                if (HandshakeDataQueue[0] == HandshakeData1 && HandshakeDataQueue[1] == HandshakeData2)
                                {
                                    isConnected = true;

                                    // generate client id
                                    Id = Guid.NewGuid().ToString();

                                    // prepare client data
                                    var primitive = new Primitive();
                                    primitive.Add.Int(HandshakeDataPrefix);
                                    primitive.Add.Int(HandshakeData3);
                                    primitive.Add.String(Id);
                                    byte[] data = primitive.GetBytes();

                                    // send ack data to client
                                    Send(ref data, MessageType.Reliable);
                                    break;
                                }

                                failMessage = "Invalid Handshake Method";
                                break;
                            }
                        }
                        else
                        {
                            if (HandshakeDataQueue.Count == 1)
                            {
                                if (HandshakeDataQueue[0] == HandshakeData3 &&
                                    !string.IsNullOrWhiteSpace(_receivedClientId))
                                {
                                    isConnected = true;
                                    Id = _receivedClientId;
                                    break;
                                }
                            }
                        }
                    }

                    Console.WriteLine($"* Connection Result: IsConnected: {isConnected}, IsServer: {IsServer}, Handshake: {HandshakeDataQueue.Count}, Id: {Id}");
                    
                    if (isConnected)
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

                    if (IsServer)
                    {
                        StartServerSideConnection(isConnected);
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