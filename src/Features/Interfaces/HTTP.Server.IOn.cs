using System;
using System.Net;

namespace Netly.Interfaces
{
    public partial class HTTP
    {
        public partial class Server
        {
            public interface IOn : IOn<HttpListener>
            {
            }
        }
    }
}