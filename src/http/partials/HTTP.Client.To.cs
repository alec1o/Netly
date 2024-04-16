using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Netly.Core;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client
        {
            private class _ITo : ITo
            {
                private _IOn On => _client._on;
                private readonly Client _client;
                private int _timeout;
                public bool IsOpened { get; private set; }

                public _ITo(Client client)
                {
                    _client = client;
                }

                public Task Fetch(string method, string url, byte[] body = null)
                {
                    if (IsOpened)
                    {
                        // NOTE: it now allow fetch multi request
                        Exception e = new Exception
                        (
                            $"[{nameof(Client)}] execute a fetch when exist a operation with same this instance. " +
                            "You must wait for release this operation, " +
                            "for handle it you can use those callbacks: Close, Fetch and, Error"
                        );

                        NETLY.Logger.PushError(e);

                        throw e;
                    }

                    // start and block operation
                    IsOpened = true;
                    On.m_onOpen(null, null);

                    return Task.Run(async () =>
                    {
                        try
                        {
                            var http = new System.Net.Http.HttpClient();

                            var host = new Uri(url);

                            var timeout = _timeout <= 0
                                ? System.Threading.Timeout.InfiniteTimeSpan
                                : TimeSpan.FromMilliseconds(_timeout);

                            var httpMethod = new HttpMethod(method.Trim().ToUpper());

                            #region Set Queries On Request

                            var queries = _client.Queries;
                            if (queries != null && queries.Count > 0)
                            {
                                var uriBuilder = new UriBuilder(host);
                                var queryBuilder = HttpUtility.ParseQueryString(uriBuilder.Query);

                                foreach (var key in queries.Keys)
                                {
                                    if (!string.IsNullOrWhiteSpace(key))
                                    {
                                        queryBuilder.Add(key, queries[key] ?? string.Empty);
                                    }
                                }

                                uriBuilder.Query = queryBuilder.ToString();
                                host = new Uri(uriBuilder.ToString());
                            }

                            #endregion

                            var message = new HttpRequestMessage(httpMethod, host);

                            var buffer = body ?? Array.Empty<byte>();
                            message.Content = new BodyContent(ref buffer);

                            #region Set Headers On Request

                            message.Headers.Clear();

                            foreach (var header in _client.Headers)
                            {
                                message.Headers.Add(header.Key, header.Value);
                            }

                            #endregion

                            http.BaseAddress = host;
                            http.Timeout = timeout;

                            On.m_onModify?.Invoke(null, http);

                            var response = await http.SendAsync(message, CancellationToken.None);

                            var request = new Request(response);

                            On.m_onFetch?.Invoke(null, request);
                        }
                        catch (Exception ex)
                        {
                            NETLY.Logger.PushError(ex);
                            On.m_onError?.Invoke(null, ex);
                        }
                        finally
                        {
                            // release operation
                            IsOpened = false;
                            On.m_onClose?.Invoke(null, null);
                        }
                    });
                }

                public Task Fetch(string method, string url, string body = null, NE.Encoding encode = NE.Encoding.UTF8)
                {
                    return Fetch(method, url, NE.GetBytes(body ?? string.Empty, encode));
                }

                public int GetTimeout() => _timeout;

                public void SetTimeout(int timeout)
                {
                    // connection is opened error.
                    if (IsOpened)
                    {
                        throw new InvalidOperationException
                        (
                            "You can modify timeout when request is on progress."
                        );
                    }

                    // invalid timeout value (is negative)
                    if (_timeout < -1)
                    {
                        throw new ArgumentOutOfRangeException
                        (
                            $"({timeout}) is invalid timeout value. it must be posetive value or (-1 or 0), (-1 or 0) means infinite timeout value (without timeout)."
                        );
                    }

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
}