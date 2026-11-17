using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Netly;
using TcpClient = Netly.TcpClient;
using UdpClient = Netly.UdpClient;

// dotnet run -c Release --project bench -- tcp 5000
// protocols: tcp, udp, rudp, ws. The count is the number of clients, the server has the same number of connections.
var protocol = args.Length > 0 ? args[0] : "tcp";
var total = args.Length > 1 ? int.Parse(args[1]) : 1000;
var rounds = args.Length > 2 ? int.Parse(args[2]) : 200;

Console.WriteLine($"Netly bench: {protocol}, {total} clients + {total} server-side, {rounds} ping-pong rounds");
Console.WriteLine($"runtime {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}, {Environment.ProcessorCount} cores");

var host = new Host(IPAddress.Loopback, FreePort(protocol));
var threads0 = Process.GetCurrentProcess().Threads.Count;
var heap0 = Collect();

switch (protocol)
{
    case "tcp": await RunTcp(); break;
    case "udp": await RunUdp(); break;
    case "rudp": await RunRudp(); break;
    case "ws": await RunWs(); break;
    default: Console.WriteLine("protocols: tcp, udp, rudp, ws"); break;
}

return;

async Task RunTcp()
{
    var server = new TcpServer();
    server.OnAccept(c => c.OnData(s => c.SendData(Read(s))));
    await Wait(w => server.OnOpen(() => w()), () => server.Open(host));

    var pong = new SemaphoreSlim(0);
    var clients = Enumerable.Range(0, total).Select(_ =>
    {
        var c = new TcpClient();
        c.OnData(_ => pong.Release());
        return c;
    }).ToList();

    var opened = 0;
    var watch = Stopwatch.StartNew();
    foreach (var c in clients)
    {
        c.OnOpen(() => Interlocked.Increment(ref opened));
        c.Open(host);
    }

    await Until(() => opened == total && server.Clients.Count == total);
    Report("connect", watch, total, threads0, heap0);

    await PingPong(i => clients[i % total].SendData(new byte[64]), pong);
    await Throughput(clients[0], pong);

    foreach (var c in clients) c.Close();
    server.Close();
}

async Task RunUdp()
{
    var server = new UdpServer();
    server.OnAccept(c => c.OnData(s => c.SendData(Read(s))));
    await Wait(w => server.OnOpen(() => w()), () => server.Open(host));

    var pong = new SemaphoreSlim(0);
    var clients = Enumerable.Range(0, total).Select(_ =>
    {
        var c = new UdpClient();
        c.OnData(_ => pong.Release());
        return c;
    }).ToList();

    var opened = 0;
    var watch = Stopwatch.StartNew();
    foreach (var c in clients)
    {
        c.OnOpen(() => Interlocked.Increment(ref opened));
        c.Open(host);
    }

    await Until(() => opened == total);
    foreach (var c in clients) c.SendData(new byte[1]);
    await Until(() => server.Clients.Count == total);
    Report("open", watch, total, threads0, heap0);

    // the first datagram of each client is echoed too
    for (var i = 0; i < total; i++) await pong.WaitAsync(TimeSpan.FromSeconds(5));

    await PingPong(i => clients[i % total].SendData(new byte[64]), pong);

    foreach (var c in clients) c.Close();
    server.Close();
}

async Task RunRudp()
{
    var server = new RudpServer();
    server.OnAccept(c => c.OnData((s, t) => c.SendData(Read(s), t)));
    await Wait(w => server.OnOpen(() => w()), () => server.Open(host));

    var pong = new SemaphoreSlim(0);
    var clients = Enumerable.Range(0, total).Select(_ =>
    {
        var c = new RudpClient();
        c.OnData((_, _) => pong.Release());
        return c;
    }).ToList();

    var opened = 0;
    var watch = Stopwatch.StartNew();
    foreach (var c in clients)
    {
        c.OnOpen(() => Interlocked.Increment(ref opened));
        c.Open(host);
    }

    await Until(() => opened == total && server.Clients.Count == total);
    Report("handshake", watch, total, threads0, heap0);

    await PingPong(i => clients[i % total].SendData(new byte[64]), pong);

    foreach (var c in clients) c.Close();
    server.Close();
}

async Task RunWs()
{
    var uri = new Uri($"ws://127.0.0.1:{host.Port}/bench");
    var server = new WebSocketServer();
    server.OnAccept(c => c.OnData((s, t) => c.SendData(Read(s), t)));
    await Wait(w => server.OnOpen(() => w()), () => server.Open(uri));

    var pong = new SemaphoreSlim(0);
    var clients = Enumerable.Range(0, total).Select(_ =>
    {
        var c = new WebSocketClient();
        c.OnData((_, _) => pong.Release());
        return c;
    }).ToList();

    var opened = 0;
    var watch = Stopwatch.StartNew();
    foreach (var c in clients)
    {
        c.OnOpen(() => Interlocked.Increment(ref opened));
        c.Open(uri);
    }

    await Until(() => opened == total && server.Clients.Count == total);
    Report("handshake", watch, total, threads0, heap0);

    await PingPong(i => clients[i % total].SendData(new byte[64]), pong);

    foreach (var c in clients) c.Close();
    server.Close();
}

// one message at a time: the latency of a round trip
async Task PingPong(Action<int> send, SemaphoreSlim pong)
{
    var times = new double[rounds];

    for (var i = 0; i < rounds; i++)
    {
        var watch = Stopwatch.StartNew();
        send(i);
        if (!await pong.WaitAsync(TimeSpan.FromSeconds(5))) throw new TimeoutException($"round {i}");
        times[i] = watch.Elapsed.TotalMilliseconds;
    }

    Array.Sort(times);
    Console.WriteLine($"latency 64 B   : avg {times.Average():F3} ms, p50 {times[rounds / 2]:F3} ms, p99 {times[(int)(rounds * 0.99)]:F3} ms");
}

async Task Throughput(TcpClient client, SemaphoreSlim pong)
{
    const int size = 1024 * 1024, count = 64;
    var payload = new byte[size];
    var watch = Stopwatch.StartNew();

    for (var i = 0; i < count; i++) client.SendData(payload);
    for (var i = 0; i < count; i++) await pong.WaitAsync(TimeSpan.FromSeconds(30));

    var mb = (double)size * count * 2 / 1024 / 1024;
    Console.WriteLine($"throughput 1 MB: {mb / watch.Elapsed.TotalSeconds:F0} MB/s (echo, both directions)");
}

void Report(string what, Stopwatch watch, int count, int threadsBefore, long heapBefore)
{
    var heap = Collect() - heapBefore;
    var threads = Process.GetCurrentProcess().Threads.Count - threadsBefore;

    Console.WriteLine($"{what,-15}: {watch.Elapsed.TotalSeconds:F2} s ({count / watch.Elapsed.TotalSeconds:F0} per second)");
    Console.WriteLine($"managed heap   : {heap / 1024 / 1024} MB, {heap / (count * 2L)} bytes per socket");
    Console.WriteLine($"threads        : +{threads}");
    Console.WriteLine($"process memory : {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB working set");
}

static long Collect()
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    return GC.GetTotalMemory(true);
}

static byte[] Read(Stream stream)
{
    var buffer = new byte[stream.Length - stream.Position];
    var read = 0;
    while (read < buffer.Length) read += stream.Read(buffer, read, buffer.Length - read);
    return buffer;
}

static async Task Until(Func<bool> condition)
{
    var limit = DateTime.UtcNow.AddMinutes(2);
    while (!condition())
    {
        if (DateTime.UtcNow > limit) throw new TimeoutException("condition");
        await Task.Delay(10);
    }
}

static async Task Wait(Action<Action> register, Action start)
{
    var done = new TaskCompletionSource();
    register(() => done.TrySetResult());
    start();
    await done.Task.WaitAsync(TimeSpan.FromSeconds(30));
}

static int FreePort(string protocol)
{
    if (protocol == "tcp" || protocol == "ws")
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
    return ((IPEndPoint)socket.LocalEndPoint).Port;
}
