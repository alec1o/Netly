using System;

namespace Netly
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal struct MiddlewareInfo : IMiddlewareInfo
            {
                public string Path { get; }
                public bool UseParams { get; }
                public Func<IRequest, IResponse, bool> Callback { get; }

                public MiddlewareInfo(string path, bool useParams, Func<IRequest, IResponse, bool> callback)
                {
                    Path = path;
                    UseParams = useParams;
                    Callback = callback;
                }
            }
        }
    }
}