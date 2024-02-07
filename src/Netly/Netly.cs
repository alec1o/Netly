namespace Netly
{
    /// <summary>
    /// Netly
    /// </summary>
    public static partial class Netly
    {
        /// <summary>
        /// Netly Name
        /// </summary>
        public const string Name = "Netly";

        /// <summary>
        /// Netly Version
        /// </summary>
        public const string Version = "4.0.0";

        /// <summary>
        /// Netly supported protocols
        /// </summary>
        public static readonly string[] Protocols = new[] { "TCP", "UDP", "HTTP", "WebSocket" };

        /// <summary>
        /// Netly Git repository
        /// </summary>
        public const string GitRepository = "https://github.com/alec1o/Netly";

        /// <summary>
        /// Netly logger hub
        /// </summary>
        public static readonly ILogger Logger;

        /// <summary>
        /// Init Netly
        /// </summary>
        static Netly()
        {
            Logger = new _Logger();
        }
    }
}