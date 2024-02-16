using System.Collections.Generic;
using System.Net.Http;
using Netly.Core;

namespace Netly
{
    public static partial class HTTP
    {
        internal interface IClient
        {
            Dictionary<string, string> Headers { get; }
            Dictionary<string, string> Queries { get; }
            
            int Timeout { get; set; }
            bool IsOpened { get; }
            
            Client.IOn On { get; }
            Client.ITo To { get; }
        }
    }
}