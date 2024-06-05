namespace Netly.Interfaces
{
    public static partial class ITCP
    {
        public interface Client
        {
            string Id { get; }
            Host Host { get; }
            bool IsOpened { get; }
            bool IsFraming { get; }
            bool IsEncrypted { get; }
            ClientTo To { get; }
            ClientOn On { get; }
        }
    }
}