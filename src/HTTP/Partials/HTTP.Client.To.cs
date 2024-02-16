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
                }

                public int GetTimeout() => _timeout;

                public void SetTimeout(int timeout)
                {
                    // success, timeout changed!
                    _timeout = timeout;
                }
                }
            }
        }
    }
}