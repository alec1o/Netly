using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using Netly.Core;

namespace Netly
{
    public partial class HTTP
    {
        internal class Request : IRequest
        {
            internal Request(HttpListenerRequest request)
            {
                {
                    Headers = new Dictionary<string, string>();
                    foreach (var headerKey in request.Headers.AllKeys)
                        Headers.Add(headerKey, request.Headers[headerKey] ?? string.Empty);
                }

                {
                    Queries = new Dictionary<string, string>();
                    foreach (var queryKey in request.QueryString.AllKeys)
                        Queries.Add(queryKey, request.QueryString[queryKey] ?? string.Empty);
                }

                {
                    var cookiesList = new List<Cookie>();
                    foreach (var cookie in request.Cookies) cookiesList.Add((Cookie)cookie);

                    Cookies = cookiesList.ToArray();
                }

                {
                    // NOTE: it will modified with method that receive request
                    Params = new Dictionary<string, string>();
                }

                {
                    Status = -1;
                    
                    Method = new HttpMethod(request.HttpMethod);

                    Url = request.Url.AbsoluteUri;

                    Path = request.Url.LocalPath;

                    LocalEndPoint = new Host(request.LocalEndPoint);

                    RemoteEndPoint = new Host(request.RemoteEndPoint);

                    IsWebSocket = request.IsWebSocketRequest;

                    IsLocalRequest = request.IsLocal;

                    IsEncrypted = request.IsSecureConnection;

                    Encoding = NE.GetProtocolFromNativeEncoding(request.ContentEncoding);

                    // TODO: detect enctype from Header
                    var enctype = Enctype.PlainText;

                    var buffer = new byte[request.ContentLength64];
                    _ = request.InputStream.Read(buffer, 0, buffer.Length);
                    Body = new Body(buffer, enctype, Encoding);
                }
            }

            internal Request(ClientWebSocket ws, Uri uri)
            {
                Url = uri.AbsoluteUri;
                // TODO: fix it
            }

            public NE.Encoding Encoding { get; }
            public Dictionary<string, string> Headers { get; }
            public Dictionary<string, string> Queries { get; }
            public Dictionary<string, string> Params { get; }
            public Cookie[] Cookies { get; }
            public HttpMethod Method { get; }
            public string Url { get; }
            public string Path { get; }
            public Host LocalEndPoint { get; }
            public Host RemoteEndPoint { get; }
            public bool IsWebSocket { get; }
            public bool IsLocalRequest { get; }
            public bool IsEncrypted { get; }
            public IBody Body { get; }
            
            public int Status { get; }
        }
    }
}