using System;
using System.Collections.Generic;

namespace Netly.Core
{
    /// <summary>
    /// Netly: MainThread
    /// </summary>
    public static class MainThread
    {
        /// <summary>
        /// Automatic clean callbacks
        /// </summary>
        public static bool Automatic { get; set; } = true;
        private static List<Action> _callbacks { get; set; } = new List<Action>();

        /// <summary>
        /// Add callback to execute on (Main/Own)Thread
        /// </summary>
        /// <param name="callback">callback</param>
        public static void Add(Action callback)
        {
            if (callback == null) return;

            if (Automatic)
                callback.Invoke();
            else
                _callbacks.Add(callback);
        }

        /// <summary>
        /// Use to clean/publish callbacks: work if (Automatic is false)
        /// </summary>
        public static void Clean()
        {
            if (Automatic is false && _callbacks.Count > 0)
            {
                for (int i = 0; i < _callbacks.Count; i++)
                {
                    _callbacks[0].Invoke();
                    _callbacks.RemoveAt(0);
                }
            }
        }
    }
}
