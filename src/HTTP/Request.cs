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
        }

    }
}