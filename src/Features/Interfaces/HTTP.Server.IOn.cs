using System.Net;

namespace Netly.Features
{
    public static partial class HTTP
    {
        public partial class Server
        {
            public interface IOn : IOn<HttpListener>
            {
            }
        }
    }
}