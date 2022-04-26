using System;
using System.Net;
using System.Net.Sockets;

namespace Zenet.Network
{
    public class EasySocket
    {
        public Socket Socket { get; protected set; }
        public Socket SocketMirror { get; set; }
        public Host Host { get; set; }
        public Protocol SocketProtocol { get; set; }

        //
        public bool IsBind { get; private set; }
        public bool IsAccept { get; private set; }
        public bool IsOpen => SocketIsOpen(Socket);
        private bool tryBind, tryOpen, tryClose, closed;
        public SocketType socketType { get; private set; }
        public ProtocolType protocolType { get; private set; }

        //
        public EasySocket(Protocol protocol)
        {           
            this.SocketProtocol = protocol;
            Init(new Host(IPAddress.Any, 0));
        }

        private void Init(Host host)
        {
            Host = host;
            socketType = (SocketProtocol == Protocol.UDP) ? SocketType.Dgram : SocketType.Stream;
            protocolType = (SocketProtocol == Protocol.UDP) ? ProtocolType.Udp : ProtocolType.Tcp;
            Socket = new Socket(Host.EndPoint.AddressFamily, socketType, protocolType);
        }

        public static bool SocketIsOpen(Socket Socket)
        {
            if (Socket == null || !Socket.Connected) return false;

            try
            {
                return !(Socket.Poll(5000, SelectMode.SelectRead) && Socket.Available == 0);
            }
            catch { return false; }
        }

        //
        public void Connect(Host host, Action success, Action<Exception> error)
        {
            if (tryOpen || IsOpen) return;
            tryOpen = true;

            Callback.Execute(() =>
            {
                try
                {
                    if (SocketMirror == null)
                    {
                        Init(host);
                    }
                    else
                    {
                        Socket = CopySocket(SocketMirror);
                    }

                    Socket.Connect(Host.EndPoint);
                    closed = false;
                    success?.Invoke();
                }
                catch(Exception e)
                {
                    error?.Invoke(e);
                }

                tryOpen = false;
            });
        }

        //
        public void Bind(Host host, Action success, Action<Exception> error, int backlog = 0)
        {
            if (tryBind || IsBind || IsOpen) return;
            tryBind = true;

            Callback.Execute(() =>
            {
                try
                {
                    if (SocketMirror == null)
                    {
                        Init(host);
                    }
                    else
                    {
                        Socket = CopySocket(SocketMirror);
                    }

                    Socket.Bind(Host.EndPoint);

                    if(SocketProtocol == Protocol.TCP)
                    {
                        Socket.Listen(backlog);
                    }
                    
                    IsBind = true;
                    closed = false;
                    success?.Invoke();
                }
                catch(Exception e)
                {
                    error?.Invoke(e);
                }

                tryBind = false;
            });
        }       

        //
        public void Close(Action callback)
        {
            if (tryClose || closed) return;
            tryClose = true;

            Callback.Execute(() =>
            {
                try
                {
                    Socket?.Close();
                }
                catch { }


                if (IsBind)
                {
                    IsAccept = false;
                    IsBind = false;
                }

                callback?.Invoke();

                tryClose = false;
            });          
        }

        public static Socket CopySocket(Socket mirror)
        {
            if (mirror == null) return null;

            var socket = new Socket(mirror.AddressFamily, mirror.SocketType, mirror.ProtocolType);

            try { socket.Blocking = mirror.Blocking; } catch { }

            try { socket.DontFragment = mirror.DontFragment; } catch { }

            try { socket.DualMode = mirror.DualMode; } catch { }

            try { socket.EnableBroadcast = mirror.EnableBroadcast; } catch { }

            try { socket.ExclusiveAddressUse = mirror.ExclusiveAddressUse; } catch { }

            try { socket.LingerState = mirror.LingerState; } catch { }

            try { socket.NoDelay = mirror.NoDelay; } catch { }

            try { socket.ReceiveBufferSize = mirror.ReceiveBufferSize; } catch { }

            try { socket.ReceiveTimeout = mirror.ReceiveTimeout; } catch { }

            try { socket.SendBufferSize = mirror.SendBufferSize; } catch { }

            try { socket.MulticastLoopback = mirror.MulticastLoopback; } catch { }

            try { socket.SendTimeout = mirror.SendTimeout; } catch { }

            try { socket.Ttl = mirror.Ttl; } catch { }

            try { socket.UseOnlyOverlappedIO = mirror.UseOnlyOverlappedIO; } catch { }

            return socket;
        }
    }
}