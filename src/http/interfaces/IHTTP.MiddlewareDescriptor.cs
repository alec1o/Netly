﻿using System;
using System.Threading.Tasks;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        /// <summary>
        ///     Middleware info container
        /// </summary>
        public interface MiddlewareDescriptor
        {
            /// <summary>
            ///     Is true when this path is custom that support params e.g.: /root/{folder}
            /// </summary>
            bool UseParams { get; }

            /// <summary>
            ///     Path e.g.: /root/home
            /// </summary>
            string Path { get; }

            /// <summary>
            ///     Handler callback
            /// </summary>
            Action<ServerRequest, ServerResponse, Action> Callback { get; }
        }
    }
}