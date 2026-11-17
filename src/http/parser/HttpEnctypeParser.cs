
namespace Netly
{
    internal partial class HttpEnctypeParser : IHttpEnctypeParser
    {
        private readonly IHttpEnctypeParser _implementation;

        public HttpEnctypeParser(HttpEnctype enctype, ref byte[] buffer)
        {
            switch (enctype)
            {
                case HttpEnctype.Json:
                {
                    _implementation = new Json(ref buffer);
                    break;
                }
                case HttpEnctype.Yaml:
                {
                    _implementation = new Yml(ref buffer);
                    break;
                }
                case HttpEnctype.UrlEncoded:
                {
                    _implementation = new UrlEncoded(ref buffer);
                    break;
                }
                case HttpEnctype.Multipart:
                {
                    _implementation = new Multipart(ref buffer);
                    break;
                }
                case HttpEnctype.MultipartRelated:
                {
                    _implementation = new MultipartRelated(ref buffer);
                    break;
                }
                default:
                {
                    _implementation = new None();
                    break;
                }
            }
        }

        public bool IsValid => _implementation.IsValid;
        public string[] Keys => _implementation.Keys;

        public IHttpEnctypeObject this[string key] => _implementation[key];
    }
}