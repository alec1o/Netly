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
            bool UseConnection { get; }
            Server.ITo To { get; }
            Server.IOn On { get; }
            IClient[] Clients { get; }
        }
    }
}