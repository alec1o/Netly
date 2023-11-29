using System;
using System.Net.Http;

namespace Netly
{
    internal interface IHttpClient
    {
        HttpMethod Method { get; }
        KeyValueContainer Headers { get; }
        KeyValueContainer Queries { get; }
        RequestBody Body { get; set; }
        void OnSuccess(Action<Request> callback);
        void OnError(Action<Exception> callback);
        void Send(string method, Uri uri);
    }
}