using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Netly.Core;

namespace Netly
{
    public class Request
    {
        public readonly HttpListenerRequest RawRequest;
        public readonly KeyValueContainer Headers;
        public readonly KeyValueContainer Queries;
        public readonly Cookie[] Cookies;
        public readonly HttpMethod Method;
        public readonly string Url;
        public readonly string Path;
        public readonly Host LocalEndPoint;
        public readonly Host RemoteEndPoint;
        public readonly bool IsWebSocket;
        public readonly bool IsLocalRequest;
        public readonly bool IsEncrypted;

        public Request(HttpListenerRequest httpListenerRequest)
        {
            RawRequest = httpListenerRequest;

            #region Headers

            Headers = new KeyValueContainer();

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

            Queries = new KeyValueContainer();

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
        }


        public bool ComparePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (string.IsNullOrWhiteSpace(Path)) return false;

            return Path.Trim() == path.Trim();
        }
    }
}