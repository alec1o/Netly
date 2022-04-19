using Zenet.Debug;
using Zenet.Network;
using Zenet.Package;
using Zenet.Network.Tcp;
/*
var s = new ServerTCP(new Host("127.0.0.1", 8080));

s.OnOpen(() => Console.WriteLine("OnOpen"));
s.OnClose(() => Console.WriteLine("OnClose"));
s.OnError((e) => Console.WriteLine("OnError"));
s.OnClientOpen((c) => Console.WriteLine("OnClientOpen"));
s.OnClientClose((c) => Console.WriteLine("OnClientClose"));
s.OnClientReceive((c, d) => Console.WriteLine("OnClientReceive"));

s.Open();


*/
S_TCP();
C_TCP();

void S_TCP()
{
    var server = new SERVER_TCP();
    server.Awake();
    server.Start();
}

void C_TCP()
{
    var client = new CLIENT_TCP();
    client.Awake();
    client.Start();
    Console.ReadLine();
    client.Open(Protocol.TCP);
    Console.ReadLine();
}
