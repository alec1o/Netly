using System;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server
        {
            /// <summary>
            /// Middleware info container
            /// </summary>
            public interface IMiddlewareInfo
            {
                /// <summary>
                /// Is true when this path is custom that support params e.g.: /root/{folder}
                /// </summary>
                bool UseParams { get; }

                /// <summary>
                /// Path e.g.: /root/home
                /// </summary>
                string Path { get; }

                /// <summary>
                /// Handler callback
                /// </summary>
                Func<IRequest, IResponse, bool> Callback { get; }
            }
        }
    }
}