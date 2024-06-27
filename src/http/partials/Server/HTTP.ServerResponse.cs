using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class ServerResponse : IHTTP.ServerResponse
        {
            private readonly HttpListenerResponse _response;
            private readonly List<byte> _bytes = new List<byte>();

            public ServerResponse(HttpListenerResponse response)
            {
                NativeResponse = response;
                IsOpened = true;
                Encoding = Encoding.UTF8;
                Headers = new Dictionary<string, string>
                {
                    { "Server", "NETLY HTTP/S" },
                    { "Content-Type", "text/html; charset=utf-8" },
                    { "X-XSS-Protection", "1; mode=block" }
                };
                Cookies = Array.Empty<Cookie>();
                _response = response;
            }

            public HttpListenerResponse NativeResponse { get; }
            public Dictionary<string, string> Headers { get; }
            public Cookie[] Cookies { get; set; }
            public Encoding Encoding { get; set; }
            public bool IsOpened { get; private set; }

            public void Send(int statusCode)
            {
                WriteAndSend(statusCode);
            }

            public void Write(byte[] byteBuffer)
            {
                _bytes.AddRange(byteBuffer);
            }

            public void Write(string textBuffer)
            {
                _bytes.AddRange(textBuffer.GetBytes(Encoding));
            }

            public void Send(int statusCode, string textBuffer)
            {
                Send(statusCode, textBuffer.GetBytes(Encoding));
            }

            public void Send(int statusCode, byte[] byteBuffer)
            {
                if (!IsOpened) return;
                _bytes.AddRange(byteBuffer);
                WriteAndSend(statusCode);
            }

            public void Redirect(string url)
            {
                const int redirectCode = 307; // Temporary Redirect
                Redirect(redirectCode, url);
            }

            public void Redirect(int redirectCode, string url)
            {
                if (!IsOpened) return;
                Headers.Add("Location", url);
                WriteAndSend(redirectCode);
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

            private void WriteAndSend(int statusCode)
            {
                if (!IsOpened) return;
                IsOpened = false;

                Task.Run(() =>
                {
                    try
                    {
                        WriteHeaders();
                        WriteCookies();

                        var buffer = _bytes.ToArray();
                        _bytes.Clear();
                        _response.StatusCode = statusCode;
                        _response.ContentEncoding = Encoding;
                        _response.ContentLength64 = buffer.Length;
                        _response.OutputStream.Write(buffer, 0, buffer.Length);
                        _response.Close();
                    }
                    catch (Exception e)
                    {
                        // TODO: Handle it
                        Console.WriteLine($"{nameof(ServerRequest)} -> {nameof(Send)}: {e}");
                    }
                });
            }
        }
    }
}