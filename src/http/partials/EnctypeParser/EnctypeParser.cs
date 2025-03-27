using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal partial class EnctypeParser : IHTTP.EnctypeParser
        {
            private readonly IHTTP.EnctypeParser _implementation;

            public EnctypeParser(Enctype enctype, ref byte[] buffer)
            {
                switch (enctype)
                {
                    case Enctype.Json:
                    {
                        _implementation = new Json(ref buffer);
                        break;
                    }
                    case Enctype.Yaml:
                    {
                        _implementation = new Yml(ref buffer);
                        break;
                    }
                    case Enctype.UrlEncoded:
                    {
                        _implementation = new UrlEncoded(ref buffer);
                        break;
                    }
                    case Enctype.Multipart:
                    {
                        _implementation = new Multipart(ref buffer);
                        break;
                    }
                    case Enctype.MultipartRelated:
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

            public IHTTP.EnctypeObject this[string key] => _implementation[key];
        }
    }
}