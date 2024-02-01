using System;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class ToHttpServer : IToHttpServer
            {
                public readonly HTTP.Server m_server;

                public bool m_isOpened;
                public Uri m_uri;
                
                public ToHttpServer(HTTP.Server server)
                {
                    this.m_server = server;
                }
                
                public void Open(Uri host)
                {
                    throw new NotImplementedException();
                }

                public void Close()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}