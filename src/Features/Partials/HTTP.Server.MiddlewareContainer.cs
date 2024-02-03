﻿using System;
using IRequest = Netly.Interfaces.HTTP.IRequest;
using IResponse = Netly.Interfaces.HTTP.IResponse;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class Server
        {
            internal struct MiddlewareContainer : Interfaces.HTTP.Server.IMiddlewareContainer
            {
                public string Path { get; }
                public Func<IRequest, IResponse, bool> Callback { get; }
                
                public MiddlewareContainer(string path, Func<IRequest, IResponse, bool> callback)
                {
                    Path = path;
                    Callback = callback;
                }
            }
        }
    }
}