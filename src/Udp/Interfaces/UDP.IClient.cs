using Netly.Core;

namespace Netly
{
    public static partial class UDP
    {
        public interface IClient
        {
            string Id { get; }
            Host Host { get; }
            bool IsOpened { get; }
            bool UseConnection { get; }
            int ConnectionTimeout { get; }
            Client.ITo To { get; }
            Client.IOn On { get; }
        }
    }
}