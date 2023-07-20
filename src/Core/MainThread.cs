using System;
using System.Collections.Generic;

namespace Netly.Core
{
    /// <summary>
    /// Netly: MainThread
    /// </summary>
    public static class MainThread
    {
        private readonly static List<Action> _callbacks = new List<Action>();
        private readonly static object _lock = new object();

        /// <summary>
        /// Automatic clean callbacks
        /// </summary>
        public static bool Automatic { get; set; } = true;

        /// <summary>
        /// Add callback to execute on (Main/Own)Thread
        /// </summary>
        /// <param name="callback">callback</param>
        public static void Add(Action callback)
        {
            if (callback == null) return;

            if (Automatic)
            {
                callback.Invoke();
            }
            else
            {
                lock(_lock)
                {
                    _callbacks.Add(callback);
                }
            }
        }

        /// <summary>
        /// Use to clean/publish callbacks: work if (Automatic is false)
        /// </summary>
        public static void Clean()
        {
            if (Automatic) return;
            
            lock(_lock)
            {
                if (_callbacks.Count > 0)
                {
                    Action[] callbackArray = _callbacks.ToArray();
                    _callbacks.Clear();

                    foreach(var action in callbackArray)
                    {
                        action?.Invoke();
                    }
                }
            }
        }
    }
}
