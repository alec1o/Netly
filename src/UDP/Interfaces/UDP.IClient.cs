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
            IClientTo To { get; }
            IClientOn On { get; }
        }
    }
}