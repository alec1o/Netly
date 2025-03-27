using System.Collections.Generic;
using Netly.Interfaces;

namespace Netly
{
    public static partial class HTTP
    {
        public partial class Client : IHTTP.Client
        {
            internal readonly ClientOn _on;
            internal readonly ClientTo _to;

            /// <summary>
            ///     Create HTTP.Client instance
            /// </summary>
            public Client()
            {
                Headers = new Dictionary<string, string>();
                Queries = new Dictionary<string, string>();

                _on = new ClientOn();
                _to = new ClientTo(this);

                On = _on;
                To = _to;

                // set default timeout value. it must be used after (_to) be created
                Timeout = 15000;
            }

            /// <summary>
            ///     Fetch Header
            /// </summary>
            public Dictionary<string, string> Headers { get; }

            /// <summary>
            ///     Fetch Queries
            /// </summary>
            public Dictionary<string, string> Queries { get; }

            /// <summary>
            ///     Fetch Timeout (Milliseconds)<br />
            ///     Default is: 15000 (15 Seconds)
            /// </summary>
            public int Timeout
            {
                get => _to.GetTimeout();
                set => _to.SetTimeout(value);
            }

            /// <summary>
            ///     Is true while fetch operation it's working
            /// </summary>
            public bool IsOpened => _to.IsOpened;

            /// <summary>
            ///     Fetch callback handler
            /// </summary>
            public IHTTP.ClientOn On { get; }

            /// <summary>
            ///     Fetch action creator
            /// </summary>
            public IHTTP.ClientTo To { get; }
        }
    }
}