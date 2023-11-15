namespace Netly
{
    public class Response
    {
        public readonly HttpListenerResponse RawResponse;

        public Response(HttpListenerResponse httpListenerResponse)
        {
            RawResponse = httpListenerResponse;
        }


    }
}