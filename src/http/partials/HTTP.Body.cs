﻿using System;
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
            public Body(ref byte[] buffer, Encoding encoding, Dictionary<string, string> header)
            {
                Binary = buffer;
                Encoding = encoding;
                Text = buffer.GetString(encoding);
                Enctype = GetEnctypeFromHeader(ref header);
                Parser = new EnctypeParser(Enctype, ref buffer);
            }

            public Enctype Enctype { get; }
            public string Text { get; }
            public byte[] Binary { get; }
            public IHTTP.EnctypeParser Parser { get; }
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