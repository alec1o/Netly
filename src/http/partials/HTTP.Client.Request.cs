using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        internal class ClientRequest : IHTTP.Request
        {
            public Encoding Encoding { get; }
            public Dictionary<string, string> Headers { get; }
            public Dictionary<string, string> Queries { get; }
            public Dictionary<string, string> Params { get; }
            public Cookie[] Cookies { get; }
            public HttpMethod Method { get; }
            public string Url { get; }
            public string Path { get; }
            public Host LocalEndPoint { get; }
            public Host RemoteEndPoint { get; }
            public bool IsWebSocket { get; }
            public bool IsLocalRequest { get; }
            public bool IsEncrypted { get; }
            public IHTTP.Body Body { get; }
            public int Status { get; }

            public ClientRequest()
            {
            }
        }
    }
}