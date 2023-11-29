using System;
using System.Net.Http;
using Netly.Core;

namespace Netly
{
    public class HttpClient : IHttpClient
    {
        public KeyValueContainer Headers { get; }
        public KeyValueContainer Queries { get; }
        public RequestBody Body { get; set; }
        public HttpMethod Method { get; }

        private EventHandler<Request> _onSuccess;
        private EventHandler<Exception> _onError;

        public HttpClient()
        {
            Headers = new KeyValueContainer();
            Queries = new KeyValueContainer();
            Body = new RequestBody();
            Method = HttpMethod.Get;
        }

        public void OnSuccess(Action<Request> callback)
        {
            if(callback == null) return;

            _onSuccess += (_, request) => MainThread.Add(() => callback?.Invoke(request));
        }

        public void OnError(Action<Exception> callback)
        {
            _onError += (_, exception) => MainThread.Add(() => callback?.Invoke(exception));
        }

        public void Send(string method, Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}