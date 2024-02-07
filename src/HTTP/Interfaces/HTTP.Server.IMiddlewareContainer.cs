using System;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server
        {
            public interface IMiddlewareContainer
            {
                string Path { get; }
                Func<IRequest, IResponse, bool> Callback { get; }
            }
        }
    }
}