using System;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal class _To : Interfaces.HTTP.Server.ITo
            {
                public readonly HTTP.Server m_server;

                public bool m_isOpened;
                public Uri m_uri;
                
                public _To(HTTP.Server server)
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