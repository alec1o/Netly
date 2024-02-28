using Netly.Core;

namespace Netly
{
    public static partial class TCP
    {
        internal interface IClient
        {
            string Id { get; }
            Host Host { get; }
            bool IsOpened { get; }
            bool IsFraming { get; }
            bool IsEncrypted { get; }
            Client.ITo To { get; }
            Client.IOn On { get; }
        }
    }
}