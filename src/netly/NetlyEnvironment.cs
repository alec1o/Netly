namespace Netly
{
    /// <summary>
    ///     Netly Library Info
    /// </summary>
    public static partial class NetlyEnvironment
    {
        /// <summary>
        ///     Netly Name
        /// </summary>
        public const string Name = "Netly";

        /// <summary>
        ///     Netly Version
        /// </summary>
        public const string Version = "4.0.0";

        /// <summary>
        ///     Netly Git repository
        /// </summary>
        public const string GitRepository = "https://github.com/alec1o/Netly";

        /// <summary>
        ///     Netly supported protocols
        /// </summary>
        public static readonly string[] Protocols = { "TCP", "UDP", "HTTP", "RUDP", "WebSocket" };

        /// <summary>
        ///     Netly logger hub
        /// </summary>
        public static readonly ILogger Logger;

        /// <summary>
        ///     Netly main thread
        /// </summary>
        public static readonly IMainThread MainThread;


        /// <summary>
        ///     Init Netly
        /// </summary>
        static NetlyEnvironment()
        {
            Logger = new MyLogger();
            MainThread = new MyMainThread();
        }
    }
}