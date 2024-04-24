using System;
using System.Text;
using System.Threading.Tasks;
using Netly.Core;
using Netly.Interfaces;

namespace Netly
{
    public static partial class RUDP
    {
        public partial class Client
        {
            private class ClientTo : IRUDP.ClientTo
            {
                private readonly Client _client;
                private readonly UDP.Client _udp;
                private DateTime _timer;
                private readonly object _timerLock;

                private ClientTo()
                {
                    _udp = new UDP.Client();
                    _timer = new DateTime();
                    _timerLock = new object();
                }

                public ClientTo(Client client) : this()
                {
                    _client = client;
                    InitRudpBehave();
                }

                public Host Host => _udp.Host;
                public bool IsOpened => _udp.IsOpened;
                private ClientOn On => _client._on;

                public Task Open(Host host)
                {
                    return _udp.To.Open(host);
                }

                public Task Close()
                {
                    return _udp.To.Close();
                }

                public void Data(byte[] data)
                {
                    _udp.To.Data(data);
                }

                public void Data(string data)
                {
                    _udp.To.Data(data);
                }

                public void Data(string data, Encoding encoding)
                {
                    _udp.To.Data(data, encoding);
                }

                public void Event(string name, byte[] data)
                {
                    _udp.To.Event(name, data);
                }

                public void Event(string name, string data)
                {
                    _udp.To.Event(name, data);
                }

                public void Event(string name, string data, Encoding encoding)
                {
                    _udp.To.Event(name, data, encoding);
                }

                private void InitRudpBehave()
                {
                    _udp.On.Data(data =>
                    {
                        // update connection timer to connection not closed
                        UpdateTimer();

                        // ignore ping data, ping data is only for internal use and isn't invoked
                        if (!IsPing(ref data))
                        {
                            // raw data received
                            On.OnData?.Invoke(null, data);
                        }
                    });

                    _udp.On.Event((name, data) =>
                    {
                        // update connection timer to connection not closed
                        UpdateTimer();

                        // ignore ping data, ping data is only for internal use and isn't invoked
                        if (!IsPing(ref data))
                        {
                            // raw data received
                            On.OnEvent?.Invoke(null, (name: name, buffer: data));
                        }
                    });
                    
                    _udp.On.Open(() =>
                    {
                        // invoke connection error: from udp to rudp
                        // TODO: only invoke connection opened if receive once data from server in timeout or invoke on error (server isn't responding)
                        throw new NotImplementedException();
                        On.OnOpen?.Invoke(null, null);
                    });

                    _udp.On.Error(exception =>
                    {
                        // invoke connection error: from udp to rudp
                        On.OnError?.Invoke(null, exception);
                    });
                    
                    _udp.On.Modify(socket =>
                    {
                        // invoke connection modify: from udp to rudp
                        On.OnModify?.Invoke(null, socket);
                    });
                    
                    _udp.On.Close(() =>
                    {
                        // invoke connection close: from udp to rudp
                        On.OnClose?.Invoke(null, null);
                    });
                }

                private void UpdateTimer()
                {
                    lock (_timerLock)
                    {
                        // is a ping data ignore this message and update timer;
                        _timer = DateTime.UtcNow.AddMilliseconds(ConstConnectionTimeout);
                    }
                }

                private bool IsPing(ref byte[] data)
                {
                    // detect if data is a ping
                    return data.Length == 1 && data[0] == ConstPingByte;
                }
            }
        }
    }
}