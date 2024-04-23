using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public partial class HTTP
    {
        internal class Response : IResponse
        {
            private readonly HttpListenerResponse _response;
            private readonly HttpResponseMessage _message;


            public Response(HttpResponseMessage message)
            {
                _message = message;
                throw new NotImplementedException();
            }
            
            public Response(HttpListenerResponse response)
            {
                IsOpened = true;
                ResponseEncoding = Encoding.UTF8;
                Headers = new Dictionary<string, string>
                {
                    { "Server", "NETLY HTTP/S" },
                    { "Content-Type", "text/html; charset=utf-8" },
                    { "X-XSS-Protection", "1; mode=block" }
                };
                Cookies = Array.Empty<Cookie>();
                _response = response;
            }

            public Dictionary<string, string> Headers { get; }
            public Cookie[] Cookies { get; set; }
            public Encoding ResponseEncoding { get; set; }
            public bool IsOpened { get; private set; }

            public void Send(int statusCode, string textBuffer)
            {
                Send(statusCode, NE.GetBytes(textBuffer, ResponseEncoding));
            }

            public void Send(int statusCode, byte[] byteBuffer)
            {
                if (!IsOpened) return;
                IsOpened = false;

                Write(statusCode, byteBuffer, ResponseEncoding);
            }

            public void Redirect(string url)
            {
                const int redirectCode = 307; // Temporary Redirect
                Redirect(redirectCode, url);
            }

            public void Redirect(int redirectCode, string url)
            {
                if (!IsOpened) return;
                IsOpened = false;

                Headers.Add("Location", url);
                Write(redirectCode, Array.Empty<byte>(), Encoding.UTF8);
            }

            public void Close()
            {
                if (!IsOpened) return;
                IsOpened = false;

                Task.Run(() =>
                {
                    WriteHeaders();
                    WriteCookies();

                    _response.Close();
                });
            }

            private void WriteHeaders()
            {
                foreach (var header in Headers) _response.AddHeader(header.Key, header.Value);
            }

            private void WriteCookies()
            {
                foreach (var cookie in Cookies) _response.Cookies.Add(cookie);
            }

            private void Write(int statusCode, byte[] buffer, Encoding encoding)
            {
                Task.Run(() =>
                {
                    try
                    {
                        WriteHeaders();
                        WriteCookies();

                        _response.StatusCode = statusCode;
                        _response.ContentEncoding = encoding;
                        _response.ContentLength64 = buffer.Length;
                        _response.OutputStream.Write(buffer, 0, buffer.Length);
                        _response.Close();
                    }
                    catch (Exception e)
                    {
                        // TODO: Handle it
                        Console.WriteLine($"{nameof(Request)} -> {nameof(Send)}: {e}");
                    }
                });
            }
        }
    }
}