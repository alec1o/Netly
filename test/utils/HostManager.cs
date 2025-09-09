using System.Net;

public static class HostManager
{
    private const int Initial = 1024;
    private const int Skip = 3;
    private static int _index = Initial;
    private static readonly object Locker = new();

    public static NHost GenerateLocalHost()
    {
        lock (Locker)
        {
            _index += Skip;
            return new NHost(IPAddress.Loopback, _index);
        }
    }
}