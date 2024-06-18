using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface ClientResponse
        {
            /// <summary>
            /// Native Response Object
            /// </summary>
            HttpResponseMessage NativeResponse { get; }

            /// <summary>
            /// Native Client Object
            /// </summary>
            HttpClient NativeClient { get; }

            /// <summary>
            ///     Response encoding
            /// </summary>
            Encoding Encoding { get; }

            /// <summary>
            ///     Response Headers
            /// </summary>
            Dictionary<string, string> Headers { get; }

            /// <summary>
            ///     Response Queries
            /// </summary>
            Dictionary<string, string> Queries { get; }

            /// <summary>
            ///     Request Http Method
            /// </summary>
            HttpMethod Method { get; }

            /// <summary>
            ///     Response Enctype
            /// </summary>
            HTTP.Enctype Enctype { get; }

            /// <summary>
            ///     Request Url
            /// </summary>
            string Url { get; }

            /// <summary>
            ///     Request Path
            /// </summary>
            string Path { get; }

            /// <summary>
            ///     Return true if request is same host
            /// </summary>
            bool IsLocalRequest { get; }

            /// <summary>
            ///     Return true if connection is encrypted e.g SSL or TLS protocol
            /// </summary>
            bool IsEncrypted { get; }

            /// <summary>
            ///     Request Body
            /// </summary>
            Body Body { get; }

            /// <summary>
            ///     Http Status Code. <br />
            ///     if value is -1 mean: (not applicable on context).
            /// </summary>
            int Status { get; }
        }
    }
}