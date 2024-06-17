using System;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface Middleware
        {
            /// <summary>
            ///     Middleware array
            /// </summary>
            MiddlewareDescriptor[] Middlewares { get; }

            /// <summary>
            ///     Add global middleware handler
            /// </summary>
            /// <param name="middleware">Middleware handler</param>
            /// <returns>true if callback added successful</returns>
            bool Add(Func<Request, Response, bool> middleware);

            /// <summary>
            ///     Add local middleware handler
            /// </summary>
            /// <param name="path">Route path</param>
            /// <param name="middleware">Middleware handler</param>
            /// <returns>true if callback added successful</returns>
            /// <returns></returns>
            bool Add(string path, Func<Request, Response, bool> middleware);
        }
    }
}