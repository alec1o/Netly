using System;
using Zenet.Manager;
using Zenet.Network;
using Zenet.Network.Tcp;
using Zenet.Package;

namespace Zenet.Debug
{
    public class SERVER_TCP : ServerBehaviour
    {
        public void Awake()
        {
            Init(Protocol.TCP, new Host("127.0.0.1", 8800));
            Init(Protocol.UDP, new Host("127.0.0.1", 9900));
        }

        public void Start()
        {
            /*
            Close(Protocol.TCP);
            Close(Protocol.UDP);
            */

            Open(Protocol.TCP);
            Open(Protocol.UDP);

            /*
            bool tcp = Exist(Protocol.TCP);
            bool udp = Exist(Protocol.UDP);
            */
        }

        public override void OnOpen(Protocol protocol)
        {
            Console.WriteLine($"[{protocol} SERVER] OnOpen");
        }

        public override void OnClose(Protocol protocol)
        {
            Console.WriteLine($"[{protocol} SERVER] OnClose");
        }

        public override void OnError(Protocol protocol, Exception e)
        {
            Console.WriteLine($"[{protocol} SERVER] OnError: {e}");
        }

        public override void OnEnter(Protocol protocol, object client)
        {
            Console.WriteLine($"[{protocol} AGENT] OnEnter");
            var c = client as AgentTCP;
            c.Send(Encoding.Bytes("hello client"), false);
            c.Send(Encoding.Bytes("close"));
        }
        
        public override void OnExit(Protocol protocol, object client)
        {
            Console.WriteLine($"[{protocol} AGENT] OnExit");
        }

        public override void OnData(Protocol protocol, object client, byte[] data)
        {
            Console.WriteLine($"[{protocol} AGENT] OnData: {Encoding.String(data)}");
        }
        
        public override void OnEvent(Protocol protocol, object client, string name, byte[] data)
        {
            Console.WriteLine($"[{protocol} AGENT] OnEvent ({name}): {Encoding.String(data)}");
        }
    }
}