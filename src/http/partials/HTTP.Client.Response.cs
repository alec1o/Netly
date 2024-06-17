using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        private class ClientResponse : IHTTP.Response
        {
            public ClientResponse()
            {
            }

            public Dictionary<string, string> Headers { get; }
            public Cookie[] Cookies { get; }
            public Encoding Encoding { get; set; }
            public bool IsOpened { get; }

            public void Send(int statusCode, string textBuffer)
            {
                throw new NotImplementedException();
            }

            public void Send(int statusCode, byte[] byteBuffer)
            {
                throw new NotImplementedException();
            }

            public void Redirect(string url)
            {
                throw new NotImplementedException();
            }

            public void Redirect(int redirectCode, string url)
            {
                throw new NotImplementedException();
            }

            public void Close()
            {
                throw new NotImplementedException();
            }
        }
    }
}