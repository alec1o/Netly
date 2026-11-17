using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Byter;

namespace Netly
{
    public sealed class HttpBody
    {
        private readonly byte[] _binary;

        private Func<Type, object> _handler;
        private readonly object _parseLock = new object();
        private string _text;

        internal HttpBody(ref byte[] buffer, Encoding encoding, Dictionary<string, string> header)
        {
            _binary = buffer;
            Encoding = encoding;
            _handler = null;
            Enctype = GetEnctypeFromHeader(ref header);
        }

        public HttpEnctype Enctype { get; }
        public byte[] Binary => _binary;
        public string Text => _text ?? (_text = _binary.GetString(Encoding));

        public T Parse<T>()
        {
            var value = _handler(typeof(T));

            if (value == default || value == null) return default;

            return (T)value;
        }

        public Encoding Encoding { get; }

        private static HttpEnctype GetEnctypeFromHeader(ref Dictionary<string, string> headers)
        {
            var key = headers.Keys.FirstOrDefault(x =>
                x.Equals("ContentType", StringComparison.OrdinalIgnoreCase) ||
                x.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
            );

            if (string.IsNullOrWhiteSpace(key) || !headers.TryGetValue(key, out var value))
                return HttpEnctype.None;

            var content = value.ToLower().Trim();

            if (content.Contains("application/x-www-form-urlencoded")) return HttpEnctype.UrlEncoded;

            if (content.Contains("multipart/form-data")) return HttpEnctype.Multipart;

            if (content.Contains("text/plain")) return HttpEnctype.PlainText;

            if (content.Contains("application/json")) return HttpEnctype.Json;

            if (content.Contains("application/xml") || content.Contains("text/xml")) return HttpEnctype.Xml;

            if (content.Contains("application/octet-stream")) return HttpEnctype.OctetStream;

            if (content.Contains("text/csv")) return HttpEnctype.Csv;

            if (content.Contains("text/html")) return HttpEnctype.Html;

            if (content.Contains("application/pdf")) return HttpEnctype.Pdf;

            if (content.Contains("application/javascript")) return HttpEnctype.Javascript;

            if (content.Contains("application/x-yaml")) return HttpEnctype.Yaml;

            if (content.Contains("application/graphql")) return HttpEnctype.GraphQL;

            if (content.Contains("application/soap+xml")) return HttpEnctype.SoapXml;

            if (content.Contains("application/zip")) return HttpEnctype.Zip;

            if (content.Contains("multipart/related")) return HttpEnctype.MultipartRelated;

            if (content.Contains("application/wasm")) return HttpEnctype.WebAssembly;

            if (content.Contains("text/markdown")) return HttpEnctype.Markdown;

            return HttpEnctype.Unknown;
        }

        public void RegisterParser(bool replaceOnMatch, Func<Type, object> parser)
        {
            lock (_parseLock)
            {
                if (_handler == null || replaceOnMatch) _handler = parser;
            }
        }
    }
}