using System;
using System.Net;
using System.Text;
using System.Threading;
using Netly.Core;

namespace Netly
{
    public class Response
    {
        public bool IsUsed { get; private set; } = false;
        public readonly HttpListenerResponse RawResponse;

        public Response(HttpListenerResponse httpListenerResponse)
        {
            IsUsed = false;
            RawResponse = httpListenerResponse;
        }

        public void Send(int statusCode, byte[] buffer)
        {
            if (IsUsed)
            {
                throw new InvalidOperationException($"{nameof(Response)}->{nameof(Send)} method was called before.");
            }
            
            IsUsed = true;
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    RawResponse.StatusCode = statusCode;
                    RawResponse.ContentEncoding = Encoding.UTF8;
                    RawResponse.ContentLength64 = buffer.Length;
                    RawResponse.OutputStream.Write(buffer, 0, buffer.Length);
                    RawResponse.Close();
                }
                catch (Exception e)
                {
                    // TODO: Handle it
                    Console.WriteLine(e);
                }
            });
        }

        public void Send(int statusCode, string buffer)
        {
            Send(statusCode, NE.GetBytes(buffer, NE.Mode.UTF8));
        }
    }
}