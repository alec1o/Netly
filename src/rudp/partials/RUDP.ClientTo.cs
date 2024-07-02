using System;
using System.Text;
using System.Threading.Tasks;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientTo : IRUDP.ClientTo
            {
                public Host Host { get; private set; }
                public bool IsOpened { get; private set; }
                private readonly Client _client;
                private struct Config
                {
                    public const byte PingBuffer = 0;
                    public const byte OpenBuffer = 128;
                    public const byte CloseBuffer = 255;
                    public const byte OpenRepeat = 32;
                    public const byte CloseRepeat = 32;
                }

                public ClientTo(Client client)
                {
                    _client = client;
                }
                
                public Task Open(Host host)
                {
                    throw new NotImplementedException();
                }

                public Task Close()
                {
                    if (IsOpened || _isOpeningOrClosing) return Task.CompletedTask;
                    _isOpeningOrClosing = true;

                    return Task.Run(() =>
                    {
                        if (IsOpened)
                        {
                            try
                            {
                                var data = new[] { Config.CloseBuffer };

                                for (var i = 0; i < Config.CloseRepeat; i++)
                                {
                                    SendRaw(_isServer ? Host : null, ref data);
                                }
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }
                        }

                        if (!_isServer)
                            try
                            {
                                _socket?.Shutdown(SocketShutdown.Both);
                                _socket?.Close();
                                _socket?.Dispose();
                            }
                            catch (Exception e)
                            {
                                NetlyEnvironment.Logger.Create(e);
                            }
                            finally
                            {
                                _socket = null;
                            }

                        _isOpeningOrClosing = false;
                        _isClosed = true;

                        On.OnClose?.Invoke(null, null);
                    });
                }

                public void Data(byte[] data, MessageType messageType)
                {
                    Send(null, data, messageType);
                }

                public void Data(string data, MessageType messageType)
                {
                    Send(null, data.GetBytes(), messageType);
                }

                public void Data(string data, MessageType messageType, Encoding encoding)
                {
                    Send(null, data.GetBytes(encoding), messageType);
                }

                public void Event(string name, byte[] data, MessageType messageType)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data), messageType);
                }

                public void Event(string name, string data, MessageType messageType)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data.GetBytes()), messageType);
                }

                public void Event(string name, string data, MessageType messageType, Encoding encoding)
                {
                    Send(null, NetlyEnvironment.EventManager.Create(name, data.GetBytes(encoding)), messageType);
                }

                private void Send(Host host, byte[] data, MessageType messageType)
                {
                    throw new NotImplementedException();
                }

                private void SendRaw(Host host, ref byte[] data)
                {
                    throw new NotImplementedException();
                }

                private bool IsConnected()
                {
                    if (_socket == null || _isClosed) return false;
                    return true;
                }
            }
        }
    }
}