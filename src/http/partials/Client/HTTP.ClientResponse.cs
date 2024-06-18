using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        internal class ClientResponse : IHTTP.ClientResponse
        {
            public ClientResponse(ref HttpResponseMessage response, ref HttpClient client)
            {
                // native response
                NativeResponse = response;

                // native client
                NativeClient = client;

                // add headers
                Headers = new Dictionary<string, string>();
                response.Headers.ToList().ForEach(x =>
                {
                    var value = string.Empty;
                    x.Value.ToList().ForEach(y => value += y);
                    Headers.Add(x.Key, value);
                });

                // encoding
                Encoding = GetEncodingFromHeader();

                // queries
                Queries = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(response.RequestMessage.RequestUri.Query))
                {
                    var builder = new UriBuilder(response.RequestMessage.RequestUri);
                    var collection = HttpUtility.ParseQueryString(builder.Query);
                    foreach (var key in collection.AllKeys) Queries.Add(key, collection[key]);
                }

                // method
                Method = response.RequestMessage.Method;

                // enctype
                Enctype = GetEnctypeFromHeader();

                // Url
                Url = response.RequestMessage.RequestUri.AbsoluteUri;

                // path
                Path = response.RequestMessage.RequestUri.LocalPath;

                // is local
                IsLocalRequest = response.RequestMessage.RequestUri.IsLoopback;

                // is encrypted
                IsEncrypted =
                    response.RequestMessage.RequestUri.Scheme.Equals("HTTPS", StringComparison.OrdinalIgnoreCase);

                // body
                Body = new Body(response.Content.ReadAsByteArrayAsync().Result, Enctype, Encoding);

                // status
                Status = (int)response.StatusCode;
            }

            public HttpResponseMessage NativeResponse { get; }
            public HttpClient NativeClient { get; }
            public Encoding Encoding { get; }
            public Dictionary<string, string> Headers { get; }
            public Dictionary<string, string> Queries { get; }
            public HttpMethod Method { get; }
            public Enctype Enctype { get; }
            public string Url { get; }
            public string Path { get; }
            public bool IsLocalRequest { get; }
            public bool IsEncrypted { get; }
            public IHTTP.Body Body { get; }
            public int Status { get; }

            private Encoding GetEncodingFromHeader()
            {
                var comparisonType = StringComparison.InvariantCultureIgnoreCase;
                var value = Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", comparisonType));
                var key = (value.Value ?? string.Empty).ToUpper();

                if (string.IsNullOrWhiteSpace(key)) return Encoding.UTF8;

                // generic
                if (key.Contains("UTF-8")) return Encoding.UTF8;
                if (key.Contains("ISO-8859-1")) return Encoding.GetEncoding("ISO-8859-1");
                if (key.Contains("ASCII")) return Encoding.ASCII;
                if (key.Contains("UTF-16")) return Encoding.Unicode;
                if (key.Contains("UTF-32")) return Encoding.UTF32;
                
                // https://en.wikipedia.org/wiki/Character_encodings_in_HTML
                return Encoding.UTF8;
            }

            private Enctype GetEnctypeFromHeader()
            {
                var comparisonType = StringComparison.InvariantCultureIgnoreCase;
                var value = Headers.FirstOrDefault(x => x.Key.Equals("Content-Type", comparisonType));
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