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
    public abstract class ClientBehaviour : UnityEngine.MonoBehaviour
#else
    public abstract class ClientBehaviour
#endif
    {
        public static bool UseEvent { get; set; } = false;
        public static bool SendAsync { get; set; } = true;
        public static Encode UseEncode { get; set; } = Encode.UTF8;
        public static ClientTCP clientTcp { get; private set; } = null;

        public virtual void OnOpen(Protocol protocol) { }
        public virtual void OnClose(Protocol protocol) { }
        public virtual void OnError(Protocol protocol, Exception e) { }

        public virtual void OnData(Protocol protocol, object client, byte[] data) { }
        public virtual void OnEvent(Protocol protocol, object client, string name, byte[] data) { }

        private bool initTCP;

        public void Init(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                if (initTCP) return;

                clientTcp.OnOpen(() => OnOpen(Protocol.TCP));
                clientTcp.OnClose(() => OnClose(Protocol.TCP));
                clientTcp.OnError((e) => OnError(Protocol.TCP, e));

                if (UseEvent)
                {
                    clientTcp.OnReceive((data) =>
                    {
                        var e = Event.Verify(data, UseEncode);

                        if (e.name == null)
                        {
                            OnData(protocol, clientTcp, data);
                        }
                        else
                        {
                            OnEvent(protocol, ClientBehaviour.clientTcp, e.name, e.value);
                        }
                    });
                }
                else
                {
                    clientTcp.OnReceive((data) => OnData(Protocol.TCP, ClientBehaviour.clientTcp, data));
                }

                initTCP = true;
            }
        }

        public void Init(Protocol protocol, Host host)
        {
            if (protocol == Protocol.TCP)
            {
                initTCP = false;
                clientTcp = new ClientTCP(host);
                Init(Protocol.TCP);
            }
        }

        public void Open(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                if (initTCP) clientTcp.Open();
            }
        }

        public void Close(Protocol protocol)
        {
            if (protocol == Protocol.TCP)
            {
                if (initTCP) clientTcp.Close();
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