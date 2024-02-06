using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly.Features
{
    public partial class HTTP
    {
        internal class Response : IResponse
        {
            public Dictionary<string, string> Headers { get; }
            public NE.Encoding Encoding { get; set; }
            public bool IsOpened { get; private set; }

            private readonly HttpListenerResponse _response;


            internal Response(HttpListenerResponse response)
            {
                IsOpened = true;
                Encoding = NE.Encoding.UTF8;
                Headers = new Dictionary<string, string>
                {
                    { "Server", "NETLY HTTP/S" },
                    { "Content-Type", "text/html; charset=utf-8" },
                    { "X-XSS-Protection", "1; mode=block" }
                };
                _response = response;
            }

            public void Send(int statusCode, string textBuffer)
            {
                Send(statusCode, NE.GetBytes(textBuffer, Encoding));
            }

            public void Send(int statusCode, byte[] byteBuffer)
            {
                if (!IsOpened) return;
                IsOpened = false;

                Write(statusCode, byteBuffer, Encoding);
            }

            private void WriteHeaders()
            {
                foreach (var header in Headers)
                {
                    _response.AddHeader(header.Key, header.Value);
                }
            }

            private void Write(int statusCode, byte[] buffer, NE.Encoding encoding)
            {
                Task.Run(() =>
                {
                    try
                    {
                        WriteHeaders();

                        _response.StatusCode = statusCode;
                        _response.ContentEncoding = NE.GetNativeEncodingFromProtocol(encoding);
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
                Write(redirectCode, Array.Empty<byte>(), NE.Encoding.UTF8);
            }

            public void Close()
            {
                if (!IsOpened) return;
                IsOpened = false;

                Task.Run(() => { _response.Close(); });
            }
        }
    }
}