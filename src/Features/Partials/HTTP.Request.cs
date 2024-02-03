using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using Netly.Core;

namespace Netly.Features
{
    public partial class HTTP
    {
        public class Request : Interfaces.HTTP.IRequest
        {
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
            public Interfaces.HTTP.IBody Body { get; }

            internal Request(HttpListenerRequest request)
            {
                {
                    Headers = new Dictionary<string, string>();
                    foreach (var headerKey in request.Headers.AllKeys)
                    {
                        Headers.Add(headerKey, request.Headers[headerKey] ?? string.Empty);
                    }
                }

                {
                    Queries = new Dictionary<string, string>();
                    foreach (var queryKey in request.QueryString.AllKeys)
                    {
                        Queries.Add(queryKey, request.QueryString[queryKey] ?? string.Empty);
                    }
                }

                {
                    var cookiesList = new List<Cookie>();
                    foreach (var cookie in request.Cookies)
                    {
                        cookiesList.Add((Cookie)cookie);
                    }

                    Cookies = cookiesList.ToArray();
                }

                {
                    // TODO: add value from parsed url
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
                    
                    Encoding = NE.GetProtocolFromNativeEncoding(request.ContentEncoding);

                    // TODO: detect enctype from Header
                    Enctype enctype = Enctype.PlainText;

                    byte[] buffer = new byte[request.ContentLength64];
                    _ = request.InputStream.Read(buffer, 0, buffer.Length);
                    Body = new HTTP.Body(buffer, enctype, Encoding);
                }
            }

            internal Request(ClientWebSocket ws, Uri uri)
            {
                Url = uri.AbsoluteUri;
                // TODO: fix it
            }
        }
    }
}