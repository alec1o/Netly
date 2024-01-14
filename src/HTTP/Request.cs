using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Netly.Core;

namespace Netly
{
    public class Request
    {
        public readonly HttpListenerRequest RawRequest;
        public readonly HttpResponseMessage RawResponse;

        public readonly KeyValueContainer<string> Headers;
        public readonly KeyValueContainer<string> Queries;
        public readonly Cookie[] Cookies;
        public readonly HttpMethod Method;
        public readonly string Url;
        public readonly string Path;
        public readonly Host LocalEndPoint;
        public readonly Host RemoteEndPoint;
        public readonly bool IsWebSocket;
        public readonly bool IsLocalRequest;
        public readonly bool IsEncrypted;
        public readonly RequestBody Body;
        public readonly int StatusCode = -1;

        internal Request(HttpResponseMessage httpResponseMessage)
        {
            Url = httpResponseMessage.RequestMessage.RequestUri.ToString();
            Cookies = Array.Empty<Cookie>();
            Headers = new KeyValueContainer<string>();
            Queries = new KeyValueContainer<string>();
            Method = httpResponseMessage.RequestMessage.Method;

            RawRequest = null;
            RawResponse = httpResponseMessage;

            StatusCode = (int)httpResponseMessage.StatusCode;

            var content = httpResponseMessage.Content;
            var buffer = content.ReadAsByteArrayAsync().Result;

            // TODO: Get enctype from Header
            // TODO: Get encoding from Header
            Body = new RequestBody(buffer, Enctype.PlainText, NE.Mode.UTF8);

            foreach (var header in httpResponseMessage.Headers)
            {
                string value = "";
                int count = header.Value.Count();

                if (count > 1)
                {
                    bool isFirst = true;
                    foreach (var i in header.Value)
                    {
                        if (!isFirst) value += "; ";
                        value += i;
                        isFirst = false;
                    }
                }
                else if (count == 1)
                {
                    value = header.Value.ToArray()[0];
                }

                Headers.Add(header.Key, value);
            }
        }

        internal Request(HttpListenerRequest httpListenerRequest)
        {
            RawRequest = httpListenerRequest;
            RawResponse = null;

            #region Headers

            Headers = new KeyValueContainer<string>();

            if (RawRequest.Headers.Count > 0)
            {
                foreach (var key in RawRequest.Headers.AllKeys)
                {
                    string value = RawRequest.Headers[key] ?? string.Empty;
                    Headers.Add(key, value);
                }
            }

            #endregion

            #region Queries

            Queries = new KeyValueContainer<string>();

            if (RawRequest.QueryString.Count > 0)
            {
                foreach (var key in RawRequest.QueryString.AllKeys)
                {
                    string value = RawRequest.QueryString[key] ?? string.Empty;
                    Queries.Add(key, value);
                }
            }

            #endregion

            #region Cookies

            var cookiesList = new List<Cookie>();

            if (RawRequest.Cookies.Count > 0)
            {
                foreach (var cookie in RawRequest.Cookies)
                {
                    cookiesList.Add((Cookie)cookie);
                }
            }

            Cookies = cookiesList.ToArray();

            #endregion

            Method = new HttpMethod(RawRequest.HttpMethod);

            Url = RawRequest.Url.AbsoluteUri;

            Path = RawRequest.Url.LocalPath;

            LocalEndPoint = new Host(RawRequest.LocalEndPoint);

            RemoteEndPoint = new Host(RawRequest.RemoteEndPoint);

            IsWebSocket = RawRequest.IsWebSocketRequest;

            IsLocalRequest = RawRequest.IsLocal;

            IsEncrypted = RawRequest.IsSecureConnection;

            #region Body

            byte[] bodyBytes = Array.Empty<byte>();

            if (RawRequest.ContentLength64 > 0)
            {
                bodyBytes = new byte[RawRequest.ContentLength64];
                _ = RawRequest.InputStream.Read(bodyBytes, 0, bodyBytes.Length);
            }

            // TODO: Get Enctype from Header
            // TODO: Get Encoding from Header
            Body = new RequestBody(bodyBytes, Enctype.PlainText, NE.Mode.UTF8);

            #endregion
        }


        public bool ComparePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (string.IsNullOrWhiteSpace(this.Path)) return false;

            return Path.Trim() == path.Trim();
        }
    }
}