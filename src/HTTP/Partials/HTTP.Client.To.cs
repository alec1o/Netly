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

                public void Fetch(string method, string url, byte[] body = null)
                {

                public void Fetch(string method, string url, string body = null, NE.Encoding encode = NE.Encoding.UTF8)
                {
                    Fetch(method, url, NE.GetBytes(body ?? string.Empty, encode));
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