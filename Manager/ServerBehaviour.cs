using System;
using System.Collections;
using System.Collections.Generic;
using Zenet.Network;
using Zenet.Package;

namespace Zenet.Manager
{
    public abstract class ServerBehaviour
    {
        public static bool UseEvent { get; set; } = false;
        public static bool SendAsync { get; set; } = true;
        public static Encode UseEncode { get; set; } = Encode.UTF8;
        public object ServerTCP;
        public object ServerUDP;

        public virtual void OnOpen(Protocol protocol) { }
        public virtual void OnClose(Protocol protocol) { }
        public virtual void OnError(Protocol protocol, Exception e) { }

        public virtual void OnEnter(Protocol protocol, object client) { }
        public virtual void OnExit(Protocol protocol, object client) { }
        public virtual void OnData(Protocol protocol, object client, byte[] data) { }
        public virtual void OnEvent(Protocol protocol, object client, string name, byte[] data) { }

        public void Init(Protocol protocol, Host host)
        {
            if (protocol == Protocol.TCP) InitTCP();
            else if (protocol == Protocol.UDP) InitUDP();
        }

        private void InitTCP() { }
        private void InitUDP() { }

        public void Open(Protocol protocol) { }
    }
}