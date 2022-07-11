using System;
using System.Threading;

namespace Netly.Core
{
    /// <summary>
    /// A module that abstracts and executes code in the background
    /// </summary>
    public static class Async
    {
        /// <summary>
        /// Execute an (Action) using (Safe ThreadPool)
        /// </summary>
        /// <param name="action">Action / Callback</param>
        /// <returns></returns>
        public static bool SafePool(Action action)
        {
            return ThreadPool.QueueUserWorkItem(_ => action(), null);
        }

        /// <summary>
        /// Execute an (Action) using (Unsafe ThreadPool)
        /// </summary>
        /// <param name="action">Action / Callback</param>
        /// <returns></returns>
        public static bool UnsafePool(Action action)
        {
            return ThreadPool.UnsafeQueueUserWorkItem(_ => action(), null);
        }
    }
}
