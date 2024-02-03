using System;
using System.Net;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly.Features
{
    public partial class HTTP
    {
        public class Response : IResponse
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

                Task.Run(() =>
                {
                    try
                    {
                        _response.StatusCode = statusCode;
                        _response.ContentEncoding = NE.GetNativeEncodingFromProtocol(Encoding);
                        _response.ContentLength64 = byteBuffer.Length;
                        _response.OutputStream.Write(byteBuffer, 0, byteBuffer.Length);
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
                if (!IsOpened) return;
                IsOpened = false;

                _response.Redirect(url);
            }

            public void Redirect(int redirectCode, string url)
            {
                if (!IsOpened) return;
                IsOpened = false;

                _response.RedirectLocation = url;
                Send(redirectCode, url);
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