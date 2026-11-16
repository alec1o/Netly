using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        internal class ClientTo : IHTTP.ClientTo
        {
            private readonly Client _client;
            private int _timeout;
            private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

            public ClientTo(Client client)
            {
                _client = client;
            }

            /// <summary>
            ///     How many times a request that failed by network or timeout is sent again. 0 means never.
            /// </summary>
            public int RetryAttempts { get; set; }

            public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

            private ClientOn On => _client._on;
            public bool IsOpened { get; private set; }

            public Task Open(string method, string url)
            {
                return Open(method, url, Array.Empty<byte>());
            }

            public Task Open(string method, string url, byte[] body)
            {
                if (IsOpened)
                {
                    // NOTE: it now allows fetch multi request
                    var e = new InvalidOperationException
                    (
                        $"[{nameof(Client)}] execute a fetch when exist a operation with same this instance. " +
                        "You must wait for release this operation, " +
                        "for handle it you can use those callbacks: Close, Fetch and, Error"
                    );

                    Logger.Singleton.Submit(e);

                    throw e;
                }

                // start and block operation
                IsOpened = true;

                // each request has its own token, a Close must not cancel the next request
                var cancellation = new CancellationTokenSource();
                _cancellationToken = cancellation;

                return Task.Run(async () =>
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
                        On.OnError?.Invoke(null, ex);
                    }
                    finally
                    {
                        // release operation
                        IsOpened = false;
                        cancellation.Dispose();
                        On.OnClose?.Invoke(null, null);
                    }
                });
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
                        ? Timeout.InfiniteTimeSpan
                        : TimeSpan.FromMilliseconds(_timeout);

                    var httpMethod = new HttpMethod(method.Trim().ToUpper());

                    #region Set Queries On Request

                    var queries = _client.Queries;
                    if (queries != null && queries.Count > 0)
                    {
                        var uriBuilder = new UriBuilder(host);
                        var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);

                        foreach (var key in queries.Keys)
                            if (!string.IsNullOrWhiteSpace(key))
                                queryBuilder.Add(key, queries[key] ?? string.Empty);

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
                        foreach (var header in _client.Headers)
                            if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
                                message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

                        #endregion

                        http.BaseAddress = host;
                        http.Timeout = timeout;

                        On.OnModify?.Invoke(null, http);

                        using (var response = await http.SendAsync(message, token))
                        {
                            var received = response;
                            var native = http;
                            var myResponse = new ClientResponse(ref received, ref native);

                            On.OnOpen?.Invoke(null, myResponse);
                        }
                    }
                }
            }

            public Task Open(string method, string url, string body)
            {
                return Open(method, url, body.GetBytes());
            }

            public Task Open(string method, string url, string body, Encoding encode)
            {
                return Open(method, url, body.GetBytes(encode));
            }

            public Task Close()
            {
                _cancellationToken.Cancel(true);
                return Task.CompletedTask;
            }

            public int GetTimeout()
            {
                return _timeout;
            }

            public void SetTimeout(int timeout)
            {
                // connection is opened error.
                if (IsOpened)
                    throw new InvalidOperationException
                    (
                        "You can modify timeout when request is on progress."
                    );

                // invalid timeout value (is negative)
                if (timeout < -1)
                    throw new ArgumentOutOfRangeException
                    (
                        $"({timeout}) is invalid timeout value. it must be positive value or (-1 or 0), (-1 or 0) means infinite timeout value (without timeout)."
                    );

                // success, timeout changed!
                _timeout = timeout;
            }
        }
    }
}