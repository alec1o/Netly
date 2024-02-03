using System.Net;
using Netly.Core;

namespace Netly.Features
{
    public partial class HTTP
    {
        public class Response : Interfaces.HTTP.IResponse
        {
            public NE.Mode Encoding { get; }
            public bool IsOpened { get; }
            public bool IsUsed { get; }


            internal Response(HttpListenerResponse response)
            {
                
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