using System;
using Zenet.Manager;
using Zenet.Network;
using Zenet.Package;

namespace Zenet.Debug
{
    public class CLIENT_TCP : ClientBehaviour
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
            Console.WriteLine($"[{protocol} CLIENT] OnOpen");
        }

        public override void OnClose(Protocol protocol)
        {
            Console.WriteLine($"[{protocol} CLIENT] OnClose");
        }

        public override void OnError(Protocol protocol, Exception e)
        {
            Console.WriteLine($"[{protocol} CLIENT] OnError: {e}");
        }

        public override void OnData(Protocol protocol, object client, byte[] data)
        {
            Console.WriteLine($"[{protocol} CLIENT] OnData: {Encoding.String(data)}");

            if(protocol == Protocol.TCP)
            {
                clientTcp.Send(Concat.Bytes(data, Encoding.Bytes(" client on: "), Encoding.Bytes(DateTime.Now.ToString()))); 
            }

            var message = Encoding.String(data);
            if(message == "close")
            {
                Close(protocol);
            }
        }
        
        public override void OnEvent(Protocol protocol, object client, string name, byte[] data)
        {
            Console.WriteLine($"[{protocol} CLIENT] OnEvent ({name}): {Encoding.String(data)}");
        }
    }
}