using System;
using System.Net.Http;
using NativeHttpClient = System.Net.Http.HttpClient;

namespace Netly
{
    internal interface IHttpClient
    {
        int Timeout { get; set; }
        HttpMethod Method { get; }
        KeyValueContainer Headers { get; }
        KeyValueContainer Queries { get; }
        RequestBody Body { get; set; }
        void OnSuccess(Action<Request> callback);
        void OnError(Action<Exception> callback);
        void OnModify(Action<NativeHttpClient> callback);
        void Send(string method, Uri uri);
    }
}