using Zenet.Debug;

S_TCP();

void S_TCP()
{
    var server = new SERVER_TCP();
    server.Awake();
    server.Start();
}

Console.ReadLine();