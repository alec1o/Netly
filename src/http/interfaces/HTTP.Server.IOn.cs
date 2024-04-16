using System.Net;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Server
        {
            /// <summary>
            ///     HTTP.Server callbacks container
            /// </summary>
            public interface IOn : IOn<HttpListener>
            {
            }
        }
    }
}