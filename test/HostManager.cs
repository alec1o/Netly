using System.Net;

namespace Netly
{
    public static class HostManager
    {
        private const int Initial = 1024;
        private const int Skip = 1;
        private static int _index = Initial;

        public static Host GenerateLocalHost()
        {
            _index += Skip;
            return new Host(IPAddress.Loopback, _index);
        }
    }
}