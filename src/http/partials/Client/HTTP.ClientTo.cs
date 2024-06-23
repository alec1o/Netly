using System;
using System.IO;
using System.Net;
using System.Net.Http;
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
            private readonly CancellationTokenSource _cancellationToken;

            public ClientTo(Client client)
            {
                _client = client;
                _cancellationToken = new CancellationTokenSource();
            }

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
                    var e = new Exception
                    (
                        $"[{nameof(Client)}] execute a fetch when exist a operation with same this instance. " +
                        "You must wait for release this operation, " +
                        "for handle it you can use those callbacks: Close, Fetch and, Error"
                    );

                    NetlyEnvironment.Logger.Create(e);

                    throw e;
                }

                // start and block operation
                IsOpened = true;

                return Task.Run(async () =>
                {
                    try
                    {
                        var http = new HttpClient();

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

                        var message = new HttpRequestMessage(httpMethod, host);

                        var buffer = body ?? Array.Empty<byte>();
                        message.Content = new BodyContent(ref buffer);

                        #region Set Headers On Request

                        message.Headers.Clear();

                        foreach (var header in _client.Headers) message.Headers.Add(header.Key, header.Value);

                        #endregion

                        http.BaseAddress = host;
                        http.Timeout = timeout;

                        On.OnModify?.Invoke(null, http);

                        var response = await http.SendAsync(message, _cancellationToken.Token);

                        var myResponse = new ClientResponse(ref response, ref http);

                        On.OnOpen?.Invoke(null, myResponse);
                    }
                    catch (Exception ex)
                    {
                        NetlyEnvironment.Logger.Create(ex);
                        On.OnError?.Invoke(null, ex);
                    }
                    finally
                    {
                        // release operation
                        IsOpened = false;
                        On.OnClose?.Invoke(null, null);
                    }
                });
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
                if (_timeout < -1)
                    throw new ArgumentOutOfRangeException
                    (
                        $"({timeout}) is invalid timeout value. it must be posetive value or (-1 or 0), (-1 or 0) means infinite timeout value (without timeout)."
                    );

                // success, timeout changed!
                _timeout = timeout;
            }

            private class BodyContent : HttpContent
            {
                private readonly byte[] _buffer;

                public BodyContent(ref byte[] buffer)
                {
                    _buffer = buffer;
                }

                protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                {
                    return stream.WriteAsync(_buffer, 0, _buffer.Length);
                }

                protected override bool TryComputeLength(out long length)
                {
                    length = _buffer.Length;
                    return true;
                }
            }
        }
    }
}