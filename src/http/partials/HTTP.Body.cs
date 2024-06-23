using System;
using System.Collections.Generic;
using System.Linq;
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

            private Enctype GetEnctypeFromHeader(ref Dictionary<string, string> headers)
            {
                // TODO: Fix Isn't working.
                var comparisonType = StringComparison.InvariantCultureIgnoreCase;
                var value = headers.FirstOrDefault(x => x.Key.Equals("Content-Type", comparisonType));
                var key = (value.Value ?? string.Empty).ToUpper();

                if (string.IsNullOrWhiteSpace(key)) return Enctype.None;

                if (key.Contains("application/x-www-form-urlencoded")) return Enctype.UrlEncoded;
                if (key.Contains("multipart/form-data")) return Enctype.Multipart;
                if (key.Contains("text/plain")) return Enctype.PlainText;

                return Enctype.None;
            }
        }
    }
}