using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly.Core
{
    public class RequestBody
    {
        public byte[] Buffer { get; internal set; }
        public int Length => Buffer.Length;
        public KeyValueContainer Form => GetForm();
        public string PlainText => GetPlainText();

        public RequestBody()
        {
            Buffer = Array.Empty<byte>();
        }
        
        public RequestBody(byte[] bodyBuffer)
        {
            Buffer = bodyBuffer ?? Array.Empty<byte>();
        }

        private string _plainText = string.Empty;
        private bool _initPlainText = false;

        private string GetPlainText()
        {
            if (_initPlainText is false)
            {
                _plainText = NE.GetString(Buffer, NE.Mode.UTF8);
                _initPlainText = true;
            }

            return _plainText;
        }

        private KeyValueContainer _form = null;
        private bool _initForm = false;

        private KeyValueContainer GetForm()
        {
            if (_initForm is false)
            {
                _form = new KeyValueContainer();
                // TODO: SEARCH Add element on form
                _initForm = true;
            }

            return _form;
        }

        public HttpContent GetHttpContent()
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
                return stream.WriteAsync(_request.Buffer, 0, _request.Length);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _request.Length;
                return true;
            }
        }
    }
}