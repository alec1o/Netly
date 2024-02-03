using System;

namespace Netly.Interfaces
{
    public partial class HTTP
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