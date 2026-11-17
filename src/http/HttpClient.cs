using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Netly
{
    /// <summary>
    ///     HTTP client. Each <see cref="Open(string, string)" /> is one request.
    /// </summary>
    public class HttpClient
    {
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private int _timeout = 15000;
        private EventHandler<HttpClientResponse> _onOpen;
        private EventHandler _onClose;
        private EventHandler<Exception> _onError;
        private EventHandler<System.Net.Http.HttpClient> _onModify;

        /* ---- INTERFACE --- */

        /// <summary>
        ///     Unique id of this client.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     True while a request is running.
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        ///     Headers sent in every request.
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>
        ///     Query parameters sent in every request.
        /// </summary>
        public Dictionary<string, string> Queries { get; } = new Dictionary<string, string>();

        /// <summary>
        ///     Milliseconds to wait for the response, 0 or -1 means no timeout. Default is 15000.
        /// </summary>
        /// <exception cref="InvalidOperationException">While a request is running.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the value is less than -1.</exception>
        public int Timeout
        {
            get => _timeout;
            set
            {
                if (IsOpened)
                    throw new InvalidOperationException($"Must not modify ({nameof(Timeout)}) while a request is running.");

                if (value < -1)
                    throw new ArgumentOutOfRangeException($"{nameof(Timeout)}: {value}");

                _timeout = value;
            }
        }

        /// <summary>
        ///     How many times a request that failed by network or timeout is sent again. Default is 0 (never).
        ///     A request canceled by <see cref="Close" /> or with an invalid url is never sent again.
        /// </summary>
        public int RetryAttempts { get; private set; }

        /// <summary>
        ///     Time to wait before each new attempt. Default is one second.
        /// </summary>
        public TimeSpan RetryDelay { get; private set; } = TimeSpan.FromSeconds(1);

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

            RetryAttempts = attempts;
            RetryDelay = delay ?? TimeSpan.FromSeconds(1);
        }

        /// <summary>
        ///     Called with the native client before each request, change the client options here.
        /// </summary>
        public void OnModify(Action<System.Net.Http.HttpClient> callback)
        {
            _onModify += (sender, client) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(client));
        }

        /// <summary>
        ///     Called when the response arrives.
        /// </summary>
        public void OnOpen(Action<HttpClientResponse> callback)
        {
            _onOpen += (sender, response) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(response));
        }

        /// <summary>
        ///     Called when the request finishes, with or without error.
        /// </summary>
        public void OnClose(Action callback)
        {
            _onClose += (sender, _) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke());
        }

        /// <summary>
        ///     Called when the request fails.
        /// </summary>
        public void OnError(Action<Exception> callback)
        {
            _onError += (sender, exception) => Dispatcher.Singleton.SubmitSafe(() => callback?.Invoke(exception));
        }

        /* ---- OPEN AND CLOSE --- */

        /// <summary>
        ///     Sends the request and returns without waiting.
        ///     The result comes by <see cref="OnOpen" /> or <see cref="OnError" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">While a request is running.</exception>
        public void Open(string method, string url)
        {
            Open(method, url, Array.Empty<byte>());
        }

        /// <summary>
        ///     Sends the request with a text body (UTF-8).
        /// </summary>
        public void Open(string method, string url, string body)
        {
            Open(method, url, Encoding.UTF8.GetBytes(body ?? string.Empty));
        }

        /// <summary>
        ///     Sends the request with a text body.
        /// </summary>
        public void Open(string method, string url, string body, Encoding encoding)
        {
            Open(method, url, (encoding ?? Encoding.UTF8).GetBytes(body ?? string.Empty));
        }

        /// <summary>
        ///     Sends the request with a body.
        /// </summary>
        public void Open(string method, string url, byte[] body)
        {
            if (IsOpened)
            {
                var e = new InvalidOperationException
                (
                    $"[{nameof(HttpClient)}] a request is already running in this instance. " +
                    $"Wait for {nameof(OnClose)} before sending another one."
                );

                Logger.Singleton.Submit(e);

                throw e;
            }

            // start and block operation
            IsOpened = true;

            // each request has its own token, a Close must not cancel the next request
            var cancellation = new CancellationTokenSource();
            _cancellationToken = cancellation;

            Task.Run(async () =>
            {
                try
                {
                    for (var attempt = 0;; attempt++)
                        try
                        {
                            await Send(method, url, body, cancellation.Token);
                            break;
                        }
                        catch (Exception ex) when (CanRetry(ex, attempt, cancellation.Token))
                        {
                            Logger.Singleton.Submit(ex.Message);
                            await Task.Delay(RetryDelay, cancellation.Token);
                        }
                }
                catch (Exception ex)
                {
                    Logger.Singleton.Submit(ex.Message);
                    _onError?.Invoke(null, ex);
                }
                finally
                {
                    // release operation
                    IsOpened = false;
                    cancellation.Dispose();
                    _onClose?.Invoke(null, null);
                }
            });
        }

        /// <summary>
        ///     Cancels the running request.
        /// </summary>
        public void Close()
        {
            try
            {
                _cancellationToken.Cancel(true);
            }
            catch (ObjectDisposedException)
            {
                // the request already finished
            }
        }

        // only network failures and timeouts, a bad url or a cancel by the user never repeat
        private bool CanRetry(Exception e, int attempt, CancellationToken token)
        {
            if (attempt >= RetryAttempts || token.IsCancellationRequested) return false;

            return e is HttpRequestException || e is TaskCanceledException || e is IOException;
        }

        private async Task Send(string method, string url, byte[] body, CancellationToken token)
        {
            using (var http = new System.Net.Http.HttpClient())
            {
                var host = new Uri(url);

                var timeout = _timeout <= 0
                    ? System.Threading.Timeout.InfiniteTimeSpan
                    : TimeSpan.FromMilliseconds(_timeout);

                var httpMethod = new HttpMethod(method.Trim().ToUpper());

                #region Set Queries On Request

                if (Queries.Count > 0)
                {
                    var uriBuilder = new UriBuilder(host);
                    var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);

                    foreach (var key in Queries.Keys)
                        if (!string.IsNullOrWhiteSpace(key))
                            queryBuilder.Add(key, Queries[key] ?? string.Empty);

                    uriBuilder.Query = queryBuilder.ToString();
                    host = new Uri(uriBuilder.ToString());
                }

                #endregion

                using (var message = new HttpRequestMessage(httpMethod, host))
                {
                    if
                    (
                        httpMethod != HttpMethod.Get &&
                        httpMethod != HttpMethod.Head &&
                        httpMethod != HttpMethod.Trace
                    )
                        message.Content = new ByteArrayContent(body ?? Array.Empty<byte>());
                    else
                        message.Content = new StringContent(string.Empty);

                    #region Set Headers On Request

                    message.Headers.Clear();
                    message.Content.Headers.Clear();
                    http.DefaultRequestHeaders.Clear();

                    // request headers first, the ones that belong to the content (Content-Type...) go there
                    foreach (var header in Headers)
                        if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
                            message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

                    #endregion

                    http.BaseAddress = host;
                    http.Timeout = timeout;

                    _onModify?.Invoke(null, http);

                    using (var response = await http.SendAsync(message, token))
                    {
                        var received = response;
                        var native = http;

                        _onOpen?.Invoke(null, new HttpClientResponse(ref received, ref native));
                    }
                }
            }
        }
    }
}
