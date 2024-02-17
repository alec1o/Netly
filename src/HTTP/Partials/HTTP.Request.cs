using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Web;
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

            internal Request(ClientWebSocket ws, Uri uri, Dictionary<string, string> headers)
            {
                // websocket client side

                {
                    Headers = headers ?? new Dictionary<string, string>();
                }

                {
                    Queries = new Dictionary<string, string>();
                    SetQueriesFromUri(uri);
                }

                {
                    var cookiesList = new List<Cookie>();

                    if (ws.Options.Cookies != null)
                    {
                        foreach (var cookie in ws.Options.Cookies.GetCookies(uri)) cookiesList.Add((Cookie)cookie);
                    }

                    Cookies = cookiesList.ToArray();
                }

                {
                    // NOTE: it will modified with method that receive request
                    Params = new Dictionary<string, string>();
                }

                {
                    // Not applicable
                    Status = -1;

                    // Not applicable
                    Method = HttpMethod.Head;

                    Url = uri.AbsoluteUri;

                    Path = uri.LocalPath;

                    // Not applicable
                    LocalEndPoint = new Host(IPAddress.Any, 0);

                    // Not applicable
                    RemoteEndPoint = new Host(IPAddress.Any, 0);

                    IsWebSocket = true;

                    IsLocalRequest = uri.IsLoopback;

                    IsEncrypted = uri.IsAbsoluteUri && uri.Scheme.ToUpper() == "WSS";

                    // Not applicable
                    Encoding = NE.Encoding.UTF8;

                    // Not applicable
                    var enctype = Enctype.PlainText;

                    // Not applicable
                    var buffer = Array.Empty<byte>();
                    
                    Body = new Body(buffer, enctype, Encoding);
                }
            }

            internal Request(HttpResponseMessage message)
            {
                Uri uri = message.RequestMessage.RequestUri;

                {
                    Headers = new Dictionary<string, string>();

                    foreach (var header in message.Headers)
                    {
                        string value = string.Empty;

                        if (header.Value.Any())
                        {
                            bool isFirst = true;
                            foreach (var key in header.Value)
                            {
                                // prepare for parsing e.g: "<..>; <..>; <..>; <...>"

                                if (!isFirst) value += "; ";
                                value += key;
                                isFirst = false;
                            }
                        }

                        Headers.Add(header.Key, value);
                    }
                }

                {
                    Queries = new Dictionary<string, string>();
                    SetQueriesFromUri(uri);
                }

                {
                    // NOTE: Not applicable
                    Cookies = Array.Empty<Cookie>();
                }

                {
                    // NOTE: Not applicable
                    Params = new Dictionary<string, string>();
                }

                {
                    Status = (int)message.StatusCode;

                    Method = message.RequestMessage.Method;

                    Url = uri.AbsoluteUri;

                    Path = uri.LocalPath;

                    // NOTE: Not applicable
                    LocalEndPoint = new Host(IPAddress.Any, 0);

                    // NOTE: Not applicable
                    RemoteEndPoint = new Host(IPAddress.Any, 0);

                    IsWebSocket = false;

                    IsLocalRequest = uri.IsAbsoluteUri && uri.IsLoopback;

                    IsEncrypted = uri.IsAbsoluteUri && uri.Scheme.ToUpper() == "HTTPS";

                    // TODO: detect encoding from Header
                    Encoding = NE.Encoding.UTF8;

                    // TODO: detect enctype from Header
                    var enctype = Enctype.PlainText;

                    byte[] buffer = message.Content.ReadAsByteArrayAsync().Result;
                    Body = new Body(buffer, enctype, Encoding);
                }
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

            /// <summary>
            /// Set queries from a exist uri
            /// </summary>
            /// <param name="uri">Uri instance</param>
            private void SetQueriesFromUri(Uri uri)
            {
                var uriBuilder = new UriBuilder(uri);
                var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);

                foreach (var queryName in queryBuilder.AllKeys)
                {
                    Queries.Add(queryName, queryBuilder[queryName]);
                }
            }
        }
    }
}