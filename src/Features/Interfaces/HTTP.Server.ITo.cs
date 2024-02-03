﻿using System;

namespace Netly.Interfaces
{
    public partial class HTTP
    {
        public partial class Server
        {
            public interface ITo
            {
                /// <summary>
                /// Open Server Connection
                /// </summary>
                /// <param name="host">Server Uri</param>
                void Open(Uri host);


                /// <summary>
                /// Close Server Connection
                /// </summary>
                void Close();
            }
        }
    }
}