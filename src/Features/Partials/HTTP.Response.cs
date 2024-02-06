using System;
using System.Net;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly.Features
{
    public partial class HTTP
    {
        internal class Response : IResponse
        {
            public NE.Encoding Encoding { get; set; }
            public bool IsOpened { get; private set; }

            private readonly HttpListenerResponse _response;


            internal Response(HttpListenerResponse response)
            {
                IsOpened = true;
                Encoding = NE.Encoding.UTF8;
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

            private void Write(int statusCode, byte[] buffer, NE.Encoding encoding)
            {
                Task.Run(() =>
                {
                    try
                    {
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

                _response.AddHeader("Location", url);
                _response.StatusCode = redirectCode;

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