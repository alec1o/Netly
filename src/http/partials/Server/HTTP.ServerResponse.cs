using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
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

            public ServerResponse(HttpListenerResponse response, HttpListenerWebSocketContext websocketContext)
            {
                NativeResponse = response;
                IsOpened = true;
                Encoding = Encoding.UTF8;
                Headers = new Dictionary<string, string>();
                Cookies = Array.Empty<Cookie>();
                _response = response;
                WebSocketContext = websocketContext;
            }

            public HttpListenerResponse NativeResponse { get; }
            public Dictionary<string, string> Headers { get; }
            public Cookie[] Cookies { get; set; }
            public Encoding Encoding { get; set; }
            public bool IsOpened { get; private set; }
            public HttpListenerWebSocketContext WebSocketContext { get; }

            public void Send(int statusCode)
            {
                if (!IsOpened) return;
                WriteAndSend(statusCode);
            }

            public void Write(byte[] byteBuffer)
            {
                if (!IsOpened) return;

                if (byteBuffer != null && byteBuffer.Length > 0) _bytes.AddRange(byteBuffer);
            }

            public void Write(string textBuffer)
            {
                if (!IsOpened) return;

                if (!string.IsNullOrEmpty(textBuffer)) _bytes.AddRange(textBuffer.GetBytes(Encoding));
            }

            public void Send(int statusCode, string textBuffer)
            {
                if (!IsOpened) return;

                if (!string.IsNullOrEmpty(textBuffer))
                {
                    byte[] buffer = null;

                    try
                    {
                        buffer = textBuffer.GetBytes(Encoding);
                    }
                    finally
                    {
                        if (buffer != null && buffer.Length > 0)
                        {
                            if (buffer.Length == 1) _bytes.Add(buffer[0]);
                            else _bytes.AddRange(buffer);
                        }
                    }
                }

                WriteAndSend(statusCode);
            }

            public void Send(int statusCode, byte[] byteBuffer)
            {
                if (!IsOpened) return;

                if (byteBuffer != null && byteBuffer.Length > 0) _bytes.AddRange(byteBuffer);

                WriteAndSend(statusCode);
            }

            public void Redirect(string url)
            {
                if (!IsOpened) return;
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
                if (Headers.Count <= 0) return;

                foreach (var header in Headers) _response.Headers[header.Key] = header.Value;
            }

            private void WriteCookies()
            {
                if (Cookies.Length <= 0) return;

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

                        var buffer = new byte[8];

                        if (_bytes.Count == 1) buffer = new[] { _bytes[0] };
                        else if (_bytes.Count > 1) buffer = _bytes.ToArray();

                        _response.StatusCode = statusCode;
                        _response.ContentEncoding = Encoding;
                        _response.ContentLength64 = buffer.Length;
                        _response.OutputStream.Write(buffer, 0, buffer.Length);
                        _response.Close();
                    }
                    catch (Exception e)
                    {
                        NetlyEnvironment.Logger.Create($"{nameof(ServerResponse)} -> {nameof(_bytes)}: {_bytes.Count}");
                        
                        try
                        {
                            _response.Close();
                            NetlyEnvironment.Logger.Create($"{nameof(ServerRequest)} -> {nameof(Send)}: {e}");
                        }
                        catch (Exception exception)
                        {
                            NetlyEnvironment.Logger.Create(
                                $"{nameof(ServerRequest)} -> {nameof(Send)}: {e} & {exception}");
                        }
                    }
                    finally
                    {
                        _bytes.Clear();
                    }
                });
            }
        }
    }
}