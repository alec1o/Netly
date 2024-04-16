using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public partial class UDP
    {
        public partial class Server
        {
            public class ServerTo : IUDP.ServerTo
            {
                private readonly Server _server;

                private bool _isClosed, _isOpeningOrClosing;

                private Socket _socket;

                private ServerTo()
                {
                    _server = null;
                    _socket = null;
                    _isClosed = true;
                    _isOpeningOrClosing = false;
                    Host = Host.Default;
                    Clients = new List<Client>();
                }

                public ServerTo(Server server) : this()
                {
                    _server = server;
                }

                private ServerOn On => _server._on;
                public bool IsOpened => !_isClosed && _socket != null;
                public Host Host { get; set; }
                public List<Client> Clients { get; set; }

                public Task Open(Host host)
                {
                    if (_isOpeningOrClosing || !_isClosed) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        try
                        {
                            _socket = new Socket(host.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                            On.OnModify?.Invoke(null, _socket);

                            _socket.Bind(host.EndPoint);

                            Host = new Host(_socket.LocalEndPoint);

                            _isClosed = false;

                            InitAccept();

                            On.OnOpen?.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            _isClosed = true;
                            On.OnError?.Invoke(null, e);
                        }
                        finally
                        {
                            _isOpeningOrClosing = false;
                        }
                    });
                }

                public Task Close()
                {
                    if (_isClosed || _isOpeningOrClosing) return Task.CompletedTask;

                    _isOpeningOrClosing = true;

                    return Task.Run(async () =>
                    {
                        try
                        {
                            _socket?.Shutdown(SocketShutdown.Both);

                            foreach (var client in Clients) await client.To.Close();

                            Clients.Clear();

                            _socket?.Close();
                            _socket?.Dispose();
                        }
                        catch
                        {
                            // Ignored
                        }
                        finally
                        {
                            _socket = null;
                            _isClosed = true;
                            _isOpeningOrClosing = false;
                            On.OnClose?.Invoke(null, null);
                        }
                    });
                }

                private void InitAccept()
                {
                    new Thread(AcceptJob)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    }.Start();
                }

                private void AcceptJob()
                {
                    var length = (int)_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                    var buffer = new byte[length];
                    var point = Host.EndPoint;

                    while (IsOpened)
                        try
                        {
                            var size = _socket.ReceiveFrom
                            (
                                buffer,
                                0,
                                buffer.Length,
                                SocketFlags.None,
                                ref point
                            );

                            if (size <= 0) continue;

                            var data = new byte[size];

                            Array.Copy(buffer, 0, data, 0, data.Length);

                            var newHost = new Host(point);

                            // Find a client
                            var client = Clients.FirstOrDefault(x => Host.Equals(newHost, x.Host));

                            if (client == null)
                            {
                                // Create new client
                                client = new Client(ref newHost, ref _socket);

                                Clients.Add(client);

                                On.OnAccept?.Invoke(null, client);

                                client.On.Close(() => Clients.Remove(client));

                                client.InitServerSide();
                            }

                            // publish data for client
                            client.OnServerBuffer(ref data);
                        }
                        catch (Exception e)
                        {
                            NETLY.Logger.PushError(e);
                        }
                }
            }
        }
    }
}