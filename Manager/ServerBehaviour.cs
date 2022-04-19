#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_WII || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE || UNITY_LUMIN || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_ANALYTICS || UNITY_WINRT
#define UNITY
#endif

using System;
using Zenet.Network;
using Zenet.Package;
using Zenet.Network.Tcp;

namespace Zenet.Manager
{
#if UNITY
    public abstract class ServerBehaviour : UnityEngine.MonoBehaviour
#else
    public abstract class ServerBehaviour
#endif
    {
        public static bool UseEvent { get; set; } = false;
        public static bool SendAsync { get; set; } = true;
        public static Encode UseEncode { get; set; } = Encode.UTF8;
        public static ServerTCP serverTcp { get; private set; } = null;

        public virtual void OnOpen(Protocol protocol) { }
        public virtual void OnClose(Protocol protocol) { }
        public virtual void OnError(Protocol protocol, Exception e) { }

        public virtual void OnEnter(Protocol protocol, object client) { }
        public virtual void OnExit(Protocol protocol, object client) { }
        public virtual void OnData(Protocol protocol, object client, byte[] data) { }
        public virtual void OnEvent(Protocol protocol, object client, string name, byte[] data) { }

        private bool initTCP;

        public void Init(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                if (initTCP) return;

                serverTcp.OnOpen(() => OnOpen(Protocol.TCP));
                serverTcp.OnClose(() => OnClose(Protocol.TCP));
                serverTcp.OnError((e) => OnError(Protocol.TCP, e));
                serverTcp.OnClientOpen((client) => OnEnter(Protocol.TCP, client));
                serverTcp.OnClientClose((client) => OnExit(Protocol.TCP, client));

                if (UseEvent)
                {                   
                    serverTcp.OnClientReceive((client, data) =>
                    {
                        var e = Event.Verify(data, UseEncode);

                        if (e.name == null)
                        {
                            OnData(protocol, client, data);
                        }
                        else
                        {
                            OnEvent(protocol, client, e.name, e.value);
                        }
                    });
                }
                else
                {
                    serverTcp.OnClientReceive((client, data) => OnData(Protocol.TCP, client, data));
                }

                initTCP = true;
            }
        }

        public void Init(Protocol protocol, Host host)
        {
            if (protocol == Protocol.TCP)
            {
                initTCP = false;
                serverTcp = new ServerTCP(host);
                Init(Protocol.TCP);
            }
        }

        public void Open(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                if (initTCP) serverTcp.Open();
            }
        }

        public void Close(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                if (initTCP) serverTcp.Close();
            }
        }

        public bool Exist(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                return initTCP;
            }

            return false;
        }
    }
}