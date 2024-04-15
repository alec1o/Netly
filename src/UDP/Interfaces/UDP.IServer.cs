using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        internal interface IServer
        {
            string Id { get; }
            Host Host { get; }
            bool IsOpened { get; }
            IServerTo To { get; }
            IServerOn On { get; }
            IClient[] Clients { get; }
        }
    }
}