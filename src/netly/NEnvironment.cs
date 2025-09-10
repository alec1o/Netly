namespace Netly
{
    /// <summary>
    ///     Provides general information about the Netly library,
    ///     including its name, version, Git repository, and supported protocols.
    /// </summary>
    public static partial class NEnvironment
    {
        /// <summary>
        ///     The name of the library.
        /// </summary>
        public const string Name = "Netly";

        /// <summary>
        ///     The current version of the library.
        /// </summary>
        public const string Version = "4.0.0";

        /// <summary>
        ///     The URL of the Netly GitHub repository.
        /// </summary>
        public const string GitRepository = "https://github.com/alec1o/Netly";

        /// <summary>
        ///     List of protocols supported by Netly.
        /// </summary>
        public static readonly string[] Protocols = { "TCP", "UDP", "HTTP", "RUDP", "WebSocket" };
    }
}