namespace Netly
{
    /// <summary>
    /// UDP Container
    /// </summary>
    public static partial class UDP
    {
        private const int StdMinTimeout = 1000;
        private const int StdConnectionTimeout = 5000;
        private const bool StdUseConnection = true;
        private const byte StdPingByte = 0;
        private static readonly byte[] StdPingBuffer = { StdPingByte };
        
        /// <summary>
        /// UDP Server
        /// </summary>
        public partial class Server
        {
        }

        /// <summary>
        /// UDP Client
        /// </summary>
        public partial class Client
        {
        }
    }
}