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
            private readonly byte[] _binary;

            private readonly Dictionary<Enctype, Func<Type, object>> _handlers;
            private readonly object _parseLock = new object();
            private string _text;

            public Body(ref byte[] buffer, Encoding encoding, Dictionary<string, string> header)
            {
                _binary = buffer;
                Encoding = encoding;
                _handlers = new Dictionary<Enctype, Func<Type, object>>();
                Enctype = GetEnctypeFromHeader(ref header);
            }

            public Enctype Enctype { get; }
            byte[] IHTTP.Body.Binary => _binary;
            string IHTTP.Body.Text => _text ?? (_text = _binary.GetString(Encoding));

            public T Parse<T>()
            {
                return Parse<T>(Enctype);
            }

            public T Parse<T>(Enctype enctype)
            {
                lock (_parseLock)
                {
                    if (_handlers.TryGetValue(enctype, out var handler))
                        if (handler != null)
                            return (T)handler(typeof(T));

                    return default;
                }
            }

            public void OnParse(Enctype enctype, bool replaceOnMatch, Func<Type, object> handler)
            {
                lock (_parseLock)
                {
                    if (!_handlers.ContainsKey(enctype))
                        _handlers.Add(enctype, handler);
                    else if (replaceOnMatch)
                        _handlers[enctype] = handler;
                }
            }

            public Encoding Encoding { get; }

            private static Enctype GetEnctypeFromHeader(ref Dictionary<string, string> headers)
            {
                var key = headers.Keys.FirstOrDefault(x =>
                    x.Equals("ContentType", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                );

                if (string.IsNullOrWhiteSpace(key) || !headers.TryGetValue(key, out var value))
                    return Enctype.None;

                var content = value.ToLower().Trim();

                if (content.Contains("application/x-www-form-urlencoded")) return Enctype.UrlEncoded;

                if (content.Contains("multipart/form-data")) return Enctype.Multipart;

                if (content.Contains("text/plain")) return Enctype.PlainText;

                if (content.Contains("application/json")) return Enctype.Json;

                if (content.Contains("application/xml") || content.Contains("text/xml")) return Enctype.Xml;

                if (content.Contains("application/octet-stream")) return Enctype.OctetStream;

                if (content.Contains("text/csv")) return Enctype.Csv;

                if (content.Contains("text/html")) return Enctype.Html;

                if (content.Contains("application/pdf")) return Enctype.Pdf;

                if (content.Contains("application/javascript")) return Enctype.Javascript;

                if (content.Contains("application/x-yaml")) return Enctype.Yaml;

                if (content.Contains("application/graphql")) return Enctype.GraphQL;

                if (content.Contains("application/soap+xml")) return Enctype.SoapXml;

                if (content.Contains("application/zip")) return Enctype.Zip;

                if (content.Contains("multipart/related")) return Enctype.MultipartRelated;

                if (content.Contains("application/wasm")) return Enctype.WebAssembly;

                if (content.Contains("text/markdown")) return Enctype.Markdown;

                return Enctype.Unknown;
            }
        }
    }
}