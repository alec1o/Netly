using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Netly.Core
{
    public class RequestBody
    {
        public readonly string Text;
        public readonly byte[] Bytes;
        public readonly Enctype Enctype;
        public readonly NE.Encoding m_encoding;
        public KeyValueContainer<string> TextForm;
        public KeyValueContainer<byte[]> BytesForm;
        public int Length => Bytes.Length;
        public HttpContent HttpContent => GetHttpContent();


        public RequestBody() : this(Array.Empty<byte>(), Enctype.PlainText, NE.Encoding.UTF8)
        {
        }

        public RequestBody(byte[] buffer, Enctype enctype, NE.Encoding encoding)
        {
            this.Bytes = buffer ?? Array.Empty<byte>();
            this.Enctype = enctype;
            this.m_encoding = encoding;
            this.Text = Bytes.Length > 0 ? NE.GetString(this.Bytes, this.m_encoding) : string.Empty;
            this.TextForm = new KeyValueContainer<string>();
            this.BytesForm = new KeyValueContainer<byte[]>();

            if (enctype != Enctype.PlainText)
            {
                #region Fill ByteForm

                // TODO: Decode enctype and get values

                #endregion

                #region Fill TextForm

                if (this.BytesForm.Length > 0)
                {
                    var textFormAsList = BytesForm.AllKeyValue
                        .Select((x) => new KeyValue<string, string>(x.Key, NE.GetString(x.Value, this.m_encoding)))
                        .ToArray();

                    this.TextForm.AddRange(textFormAsList);
                }

                #endregion
            }
        }

        private HttpContent GetHttpContent()
        {
            return new InternalHttpContent(this);
        }

        private class InternalHttpContent : HttpContent
        {
            private readonly RequestBody _request;

            public InternalHttpContent(RequestBody request)
            {
                _request = request;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return stream.WriteAsync(_request.Bytes, 0, _request.Length);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _request.Length;
                return true;
            }
        }
    }
}