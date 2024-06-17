using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                {
                    NativeResponse = response;
                }   
                
                // native client
                {
                    NativeClient = client;
                }
                
                // add headers
                {
                    Headers = new Dictionary<string, string>();

                    foreach (var header in response.Headers)
                    {
                        string value = String.Empty;

                        header.Value.ToList().ForEach(x => value += x);

                        Headers.Add(header.Key, value);
                    }
                }

                // encoding
                {
                    // TODO: Get encoding from response header or set UTF8 as default
                    Encoding = Encoding.UTF8;
                }

                // queries
                {
                    // TODO: impl query
                    Queries = new Dictionary<string, string>();
                }

                // method
                {
                    Method = response.RequestMessage.Method;
                }

                // enctype
                {
                    // TODO: get enctype from header or None
                    Enctype = Enctype.None;
                }

                // Url
                {
                    // TODO: impl url
                    // Url = response.RequestMessage.RequestUri;
                    Url = string.Empty;
                }

                // path
                {
                    // TODO: iml path
                    // Path = response.RequestMessage.RequestUri;
                    Path = string.Empty + "";
                }

                // is local
                {
                    IsLocalRequest = response.RequestMessage.RequestUri.IsLoopback;
                }

                // is encrypted
                {
                    IsEncrypted =
                        response.RequestMessage.RequestUri.Scheme.Equals("HTTPS", StringComparison.OrdinalIgnoreCase);
                }

                // body
                {
                    byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                    Body = new Body(buffer, Enctype, this.Encoding);
                }

                // status
                {
                    Status = (int)response.StatusCode;
                }
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
        }
    }
}