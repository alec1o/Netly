using System;
using System.Collections.Generic;
using System.Text;
using Byter;
using Netly.Core;

namespace Netly
{
    public partial class HTTP
    {
        internal class Body : IBody
        {
            public Body(byte[] buffer, Enctype enctype, Encoding encoding)
            {
                Binary = buffer;
                Enctype = enctype;
                Text = buffer.GetString(encoding);
                TextQueries = new Dictionary<string, string>();
                BinaryQueries = new Dictionary<string, byte[]>();

                switch (enctype)
                {
                    case Enctype.PlainText:
                        // None, Don't have enctype
                        break;
                    case Enctype.Multipart:
                        // TODO: parse buffer for Enctype.Multipart
                        break;
                    case Enctype.UrlEncoded:
                        // TODO: parse buffer for Enctype.UrlEncoded
                        break;
                    default:
                        throw new NotImplementedException(enctype.ToString());
                }
            }

            public Enctype Enctype { get; }
            public string Text { get; }
            public byte[] Binary { get; }
            public Dictionary<string, string> TextQueries { get; }
            public Dictionary<string, byte[]> BinaryQueries { get; }
        }
    }
}