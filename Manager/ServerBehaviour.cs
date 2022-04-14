using System;
using Zenet.Network;
using Zenet.Package;
using Zenet.Network.Tcp;

namespace Zenet.Manager
{
    public abstract class ServerBehaviour
    {
        public static bool UseEvent { get; set; } = false;
        public static bool SendAsync { get; set; } = true;
        public static Encode UseEncode { get; set; } = Encode.UTF8;
        public static ServerTCP ServerTCP;
        public static object ServerUDP;
        private bool initUDP, initTCP;

        public virtual void OnOpen(Protocol protocol) { }
        public virtual void OnClose(Protocol protocol) { }
        public virtual void OnError(Protocol protocol, Exception e) { }

        public virtual void OnEnter(Protocol protocol, object client) { }
        public virtual void OnExit(Protocol protocol, object client) { }
        public virtual void OnData(Protocol protocol, object client, byte[] data) { }
        public virtual void OnEvent(Protocol protocol, object client, string name, byte[] data) { }

        public void Init()
        {
            InitTCP();
            InitUDP();
        }

        public void Init(Protocol protocol, Host host)
        {
            if (protocol == Protocol.TCP)
            {
                ServerTCP = new ServerTCP(host);
                initTCP = false;
                InitTCP();
            }
            else if (protocol == Protocol.UDP) InitUDP();
        }

        private void InitTCP()
        {
            if (initTCP || ServerTCP == null) return;

            ServerTCP.OnOpen(() => OnOpen(Protocol.TCP));
            ServerTCP.OnClose(() => OnClose(Protocol.TCP));
            ServerTCP.OnError((e) => OnError(Protocol.TCP, e));

            ServerTCP.OnClientOpen((client) =>
            {
                Callback.Execute(() => OnEnter(Protocol.TCP, client));
            });

            ServerTCP.OnClientClose((client) =>
            {
                Callback.Execute(() => OnExit(Protocol.TCP, client));
            });

            ServerTCP.OnClientReceive((client, data) =>
            {
                Callback.Execute(() => OnData(Protocol.TCP, client, data));
            });

            initTCP = true;
        }

        private void InitUDP()
        {
            // ...
        }

        public void Open(Protocol protocol)
        {
            if (!initTCP && !initUDP) return;

            switch (protocol)
            {
                case Protocol.TCP:

                    if (initTCP)
                    {
                        ServerTCP.Open();
                    }

                    break;

                case Protocol.UDP:

                    if (initUDP)
                    {
                        // ...
                    }

                    break;

                default: return;
            }
        }

        public void Close(Protocol protocol)
        {
            if (!initTCP && !initUDP) return;

            switch (protocol)
            {
                case Protocol.TCP:

                    if (initTCP)
                    {
                        ServerTCP.Close();
                    }

                    break;

                case Protocol.UDP:

                    if (initUDP)
                    {
                        // ...
                    }

                    break;

                default: return;
            }
        }

        public bool Exist(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.TCP: return initTCP;
                case Protocol.UDP: return initUDP;
                default: return false;
            }
        }

        public bool Opened(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.TCP:

                    if (initTCP)
                    {
                        return ServerTCP.Opened;
                    }

                    return false;

                case Protocol.UDP:

                    if (initUDP)
                    {
                        // ...
                    }

                    return false;

                default: return false;
            }
        }
    }
}