using System.Net;
using Netly.Core;

namespace Netly.Features
{
    public partial class HTTP
    {
        public class Response : Interfaces.HTTP.IResponse
        {
            public NE.Mode Encoding { get; set; }
            public bool IsOpened { get; }

            private HttpListenerResponse _response;


            internal Response(HttpListenerResponse response)
            {
                IsOpened = true;
                Encoding = NE.Mode.UTF8;
                _response = response;
            }
            
            public void Send(int statusCode, string textBuffer)
            {
                throw new System.NotImplementedException();
            }

            public void Send(int statusCode, byte[] byteBuffer)
            {
                throw new System.NotImplementedException();
            }

            public void Redirect(string url)
            {
                throw new System.NotImplementedException();
            }

            public void Redirect(int redirectCode, string url)
            {
                throw new System.NotImplementedException();
            }

            public void Close()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}