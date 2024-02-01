using System;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class HttpMap : IMap
            {
                public readonly HTTP.Server m_server;
                public HttpMap(HTTP.Server server)
                {
                    this.m_server = server;
                }
                
                public void WebSocket(string path, Action<IRequest, IWebsocketClient> callback)
                {
                    throw new NotImplementedException();
                }

                public void All(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Get(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Put(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Head(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Post(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Patch(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Delete(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Trace(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }

                public void Options(string path, Action<IRequest, IResponse> callback)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}