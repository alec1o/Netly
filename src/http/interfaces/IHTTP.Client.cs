using System.Collections.Generic;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        internal interface Client
        {
            Dictionary<string, string> Headers { get; }
            Dictionary<string, string> Queries { get; }

            int Timeout { get; set; }
            bool IsOpened { get; }

            ClientOn On { get; }
            ClientTo To { get; }
        }
    }
}