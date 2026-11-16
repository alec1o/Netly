using System;
using System.Collections.Generic;
using System.Text;
using Netly.Interfaces;

namespace Netly
{
    /// <summary>
    ///     HTTP client. Each <see cref="Open(string, string)" /> is one request.
    /// </summary>
    public class HttpClient
    {
        private readonly HTTP.Client _inner = new HTTP.Client();

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this client.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while a request is running.
        /// </summary>
        public bool IsOpened => _inner.IsOpened;

        /// <summary>
        ///     Headers sent in every request.
        /// </summary>
        public Dictionary<string, string> Headers => _inner.Headers;

        /// <summary>
        ///     Query parameters sent in every request.
        /// </summary>
        public Dictionary<string, string> Queries => _inner.Queries;

        /// <summary>
        ///     Milliseconds to wait for the response. Default is 15000.
        /// </summary>
        public int Timeout
        {
            get => _inner.Timeout;
            set => _inner.Timeout = value;
        }

        /// <summary>
        ///     How many times a request that failed by network or timeout is sent again. Default is 0 (never).
        ///     A request canceled by <see cref="Close" /> or with an invalid url is never sent again.
        /// </summary>
        public int RetryAttempts => _inner._to.RetryAttempts;

        /// <summary>
        ///     Time to wait before each new attempt. Default is one second.
        /// </summary>
        public TimeSpan RetryDelay => _inner._to.RetryDelay;

        /// <summary>
        ///     Sends again a request that failed by network or timeout, <see cref="OnError" /> is called only when
        ///     all the attempts fail.
        /// </summary>
        /// <param name="attempts">How many times to send again. 0 turns it off.</param>
        /// <param name="delay">Time to wait before each new attempt. Null means one second.</param>
        /// <exception cref="InvalidOperationException">While a request is running.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="attempts" /> or <paramref name="delay" /> is negative.</exception>
        public void UseRetry(int attempts, TimeSpan? delay = null)
        {
            if (IsOpened)
                throw new InvalidOperationException($"Must not modify ({nameof(RetryAttempts)}) while a request is running.");

            if (attempts < 0)
                throw new ArgumentOutOfRangeException($"{nameof(attempts)}: {attempts}");

            if (delay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException($"{nameof(delay)}: {delay}");

            _inner._to.RetryAttempts = attempts;
            _inner._to.RetryDelay = delay ?? TimeSpan.FromSeconds(1);
        }

        /// <summary>
        ///     Called with the native client before each request, change the client options here.
        /// </summary>
        public void OnModify(Action<System.Net.Http.HttpClient> callback)
        {
            _inner.On.Modify(callback);
        }

        /// <summary>
        ///     Called when the response arrives.
        /// </summary>
        public void OnOpen(Action<IHTTP.ClientResponse> callback)
        {
            _inner.On.Open(callback);
        }

        /// <summary>
        ///     Called when the request finishes, with or without error.
        /// </summary>
        public void OnClose(Action callback)
        {
            _inner.On.Close(callback);
        }

        /// <summary>
        ///     Called when the request fails.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _inner.On.Error(callback);
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Sends the request and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        public void Open(string method, string url)
        {
            _ = _inner.To.Open(method, url);
        }

        /// <summary>
        ///     Sends the request with a body.
        /// </summary>
        public void Open(string method, string url, byte[] body)
        {
            _ = _inner.To.Open(method, url, body);
        }

        /// <summary>
        ///     Sends the request with a text body (UTF-8).
        /// </summary>
        public void Open(string method, string url, string body)
        {
            _ = _inner.To.Open(method, url, body);
        }

        /// <summary>
        ///     Sends the request with a text body.
        /// </summary>
        public void Open(string method, string url, string body, Encoding encoding)
        {
            _ = _inner.To.Open(method, url, body, encoding);
        }

        /// <summary>
        ///     Cancels the running request.
        /// </summary>
        public void Close()
        {
            _ = _inner.To.Close();
        }
    }
}
