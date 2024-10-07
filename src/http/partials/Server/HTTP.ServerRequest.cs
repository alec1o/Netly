using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Web;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class ServerRequest : IHTTP.ServerRequest
        {
            internal ServerRequest(HttpListenerRequest request)
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
                    // NOTE: it will modify with method that receive request
                    Params = new Dictionary<string, string>();
                }

                {
                    Method = new HttpMethod(request.HttpMethod);

                    Url = request.Url.AbsoluteUri;

                    Path = request.Url.LocalPath;

                    LocalEndPoint = new Host(request.LocalEndPoint);

                    RemoteEndPoint = new Host(request.RemoteEndPoint);

                    IsWebSocket = request.IsWebSocketRequest;

                    IsLocalRequest = request.IsLocal;

                    IsEncrypted = request.IsSecureConnection;

                    Encoding = request.ContentEncoding;

                    var enctype = GetEnctypeFromHeader();

                    var buffer = new byte[request.ContentLength64];
                    _ = request.InputStream.Read(buffer, 0, buffer.Length);
                    Body = new Body(ref buffer, Encoding, Headers);
                }
            }

            internal ServerRequest(ClientWebSocket ws, Uri uri, Dictionary<string, string> headers)
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
                        foreach (var cookie in ws.Options.Cookies.GetCookies(uri))
                            cookiesList.Add((Cookie)cookie);

                    Cookies = cookiesList.ToArray();
                }

                {
                    // NOTE: it will modify with method that receive request
                    Params = new Dictionary<string, string>();
                }

                {
                    // Not applicable
                    Method = HttpMethod.Get;

                    Url = uri.AbsoluteUri;

                    Path = uri.LocalPath;

                    // Not applicable
                    LocalEndPoint = new Host(IPAddress.Any, 0);

                    // Not applicable
                    RemoteEndPoint = new Host(IPAddress.Any, 0);

                    IsWebSocket = true;

                    IsLocalRequest = uri.IsLoopback;

                    IsEncrypted = uri.IsAbsoluteUri && uri.Scheme.ToUpper() == "WSS";

                    Encoding = GetEncodingFromHeader();

                    // Not applicable
                    var buffer = Array.Empty<byte>();

                    Body = new Body(ref buffer, Encoding, Headers);
                }
            }

            internal ServerRequest(HttpResponseMessage message)
            {
                var uri = message.RequestMessage.RequestUri;

                {
                    Headers = new Dictionary<string, string>();

                    foreach (var header in message.Headers)
                    {
                        var value = string.Empty;

                        if (header.Value.Any())
                        {
                            var isFirst = true;
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

                    Encoding = GetEncodingFromHeader();

                    var buffer = message.Content.ReadAsByteArrayAsync().Result;

                    Body = new Body(ref buffer, Encoding, Headers);
                }
            }

            public Encoding Encoding { get; }
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
            public IHTTP.Body Body { get; }
            public Enctype Enctype => Body.Enctype;

            /// <summary>
            ///     Set queries from an exist uri
            /// </summary>
            /// <param name="uri">Uri instance</param>
            private void SetQueriesFromUri(Uri uri)
            {
                var uriBuilder = new UriBuilder(uri);
                var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);

                foreach (var queryName in queryBuilder.AllKeys) Queries.Add(queryName, queryBuilder[queryName]);
            }

            private Encoding GetEncodingFromHeader()
            {
                var comparisonType = StringComparison.InvariantCultureIgnoreCase;
                var value = Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", comparisonType));
                var key = (value.Value ?? string.Empty).ToUpper();

                if (string.IsNullOrWhiteSpace(key)) return Encoding.UTF8;

                // generic
                if (key.Contains("UTF-8")) return Encoding.UTF8;
                if (key.Contains("ISO-8859-1")) return Encoding.GetEncoding("ISO-8859-1");
                if (key.Contains("ASCII")) return Encoding.ASCII;
                if (key.Contains("UTF-16")) return Encoding.Unicode;
                if (key.Contains("UTF-32")) return Encoding.UTF32;

                // https://en.wikipedia.org/wiki/Character_encodings_in_HTML
                return Encoding.UTF8;
            }

            private Enctype GetEnctypeFromHeader()
            {
                var comparisonType = StringComparison.InvariantCultureIgnoreCase;
                var value = Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", comparisonType));
                var key = (value.Value ?? string.Empty).ToUpper();

                if (string.IsNullOrWhiteSpace(key)) return Enctype.None;

                if (key.Contains("application/x-www-form-urlencoded")) return Enctype.UrlEncoded;
                if (key.Contains("multipart/form-data")) return Enctype.Multipart;
                if (key.Contains("text/plain")) return Enctype.PlainText;

                return Enctype.None;
            }
        }
    }
}