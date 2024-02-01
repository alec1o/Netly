﻿using System;
using System.Collections.Generic;

namespace Netly.Interfaces
{
    public interface IMiddleware
    {
        /// <summary>
        /// Middleware array
        /// </summary>
        Dictionary<string, Func<IRequest, IResponse, bool>>[] Middlewares { get; }

        /// <summary>
        /// Add global middleware handler
        /// </summary>
        /// <param name="middleware">Middleware handler</param>
        /// <returns>true if callback added successful</returns>
        bool Add(Func<IRequest, IResponse, bool> middleware);

        /// <summary>
        /// Add local middleware handler
        /// </summary>
        /// <param name="path">Route path</param>
        /// <param name="middleware">Middleware handler</param>
        /// <returns>true if callback added successful</returns>
        /// <returns></returns>
        bool Add(string path, Func<IRequest, IResponse, bool> middleware);
    }
}