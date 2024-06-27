using System.Collections.Generic;
using System.Text;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class Body : IHTTP.Body
        {
            public Enctype Enctype { get; }
            public string Text { get; }
            public byte[] Binary { get; }
            public Dictionary<string, string> TextQueries { get; }
            public Dictionary<string, byte[]> BinaryQueries { get; }


            public Body(byte[] buffer, Encoding encoding, Dictionary<string, string> header)
            {
                Binary = buffer;
                Enctype = GetEnctypeFromHeader(ref header);
                Text = buffer.GetString(encoding);
                TextQueries = new Dictionary<string, string>();
                BinaryQueries = new Dictionary<string, byte[]>();

                switch (Enctype)
                {
                    case Enctype.Multipart:
                    {
                        // TODO: parse buffer for Enctype.Multipart
                        break;
                    }
                    case Enctype.UrlEncoded:
                    {
                        // TODO: parse buffer for Enctype.UrlEncoded
                        break;
                    }
                    case Enctype.PlainText:
                    case Enctype.None:
                    default:
                    {
                        // N/A
                        break;
                    }
                }
            }

            private Enctype GetEnctypeFromHeader(ref Dictionary<string, string> _)
            {
                // TODO: Implement GetEnctypeFromHeader
               return Enctype.None;
            }
        }
    }
}