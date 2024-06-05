using System.Net;

public static class HostManager
{
    private const int Initial = 1024;
    private const int Skip = 2;
    private static int _index = Initial;

    public static Host GenerateLocalHost()
    {
        _index += Skip;
        return new Host(IPAddress.Loopback, _index);
    }
}