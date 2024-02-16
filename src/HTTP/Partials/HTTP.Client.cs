using System.Collections.Generic;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client : IClient
        {
            /// <summary>
            /// Fetch Header
            /// </summary>
            public Dictionary<string, string> Headers { get; }

            /// <summary>
            /// Fetch Queries
            /// </summary>
            public Dictionary<string, string> Queries { get; }

            /// <summary>
            /// Fetch Timeout (Milliseconds)<br/>
            /// Default is: 15000 (15 Seconds)
            /// </summary>
            public int Timeout
            {
                get => _to.GetTimeout();
                set => _to.SetTimeout(value);
            }

            /// <summary>
            /// Is true while fetch operation it's working
            /// </summary>
            public bool IsOpened => _to.IsOpened;

            /// <summary>
            /// Fetch callback handler
            /// </summary>
            public IOn On { get; }

            /// <summary>
            /// Fetch action creator
            /// </summary>
            public ITo To { get; }

            private readonly _IOn _on;
            private readonly _ITo _to;

            /// <summary>
            /// Create HTTP.Client instance
            /// </summary>
            public Client()
            {
            }
        }
    }
}