﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface ServerRequest
        {
            /// <summary>
            ///     Request encoding
            /// </summary>
            Encoding Encoding { get; }

            /// <summary>
            ///     Request Headers
            /// </summary>
            Dictionary<string, string> Headers { get; }

            /// <summary>
            ///     Request Queries
            /// </summary>
            Dictionary<string, string> Queries { get; }

            /// <summary>
            ///     Request Params
            /// </summary>
            Dictionary<string, string> Params { get; }

            /// <summary>
            ///     Request Cookies
            /// </summary>
            Cookie[] Cookies { get; }

            /// <summary>
            ///     Request Http Method
            /// </summary>
            HttpMethod Method { get; }

            /// <summary>
            ///     Request Url
            /// </summary>
            string Url { get; }

            /// <summary>
            ///     Request Path
            /// </summary>
            string Path { get; }

            /// <summary>
            ///     Request (local end point)
            /// </summary>
            Host LocalEndPoint { get; }

            /// <summary>
            ///     Request (remote end point)
            /// </summary>
            Host RemoteEndPoint { get; }

            /// <summary>
            ///     Return true if request is websocket
            /// </summary>
            bool IsWebSocket { get; }

            /// <summary>
            ///     Return true if request is same host
            /// </summary>
            bool IsLocalRequest { get; }

            /// <summary>
            ///     Return true if connection is encrypted e.g. SSL or TLS protocol
            /// </summary>
            bool IsEncrypted { get; }

            /// <summary>
            ///     Request Body
            /// </summary>
            Body Body { get; }
            

            /// <summary>
            ///     Request Enctype
            /// </summary>
            HTTP.Enctype Enctype { get; }
        }
    }
}