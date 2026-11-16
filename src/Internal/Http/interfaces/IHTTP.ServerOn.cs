using System.Net;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        /// <summary>
        ///     HTTP.Server callbacks container
        /// </summary>
        public interface ServerOn : IOn<HttpListener>
        {
        }
    }
}