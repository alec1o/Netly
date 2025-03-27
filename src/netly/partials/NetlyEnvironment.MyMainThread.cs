using System;
using System.Collections.Generic;

namespace Netly
{
    public partial class NetlyEnvironment
    {
        /// <summary>
        ///     Netly: MainThread
        /// </summary>
        private class MyMainThread : IMainThread
        {
            private readonly List<Action> _callbacks = new List<Action>();
            private readonly object _lock = new object();

            /// <summary>
            ///     Automatic dispatch callbacks
            /// </summary>
            public bool IsAutomatic { get; set; } = true;

            /// <summary>
            ///     Add callback to execute on (Main/Own)Thread
            /// </summary>
            /// <param name="callback">callback</param>
            public void Add(Action callback)
            {
                if (callback == null) return;

                if (IsAutomatic)
                    callback.Invoke();
                else
                    lock (_lock)
                    {
                        _callbacks.Add(callback);
                    }
            }

            /// <summary>
            ///     Use to clean/publish/dispatch callbacks <br />
            ///     WARNING: only if "Automatic == false"
            /// </summary>
            public void Dispatch()
            {
                if (IsAutomatic) return;

                var actions = Array.Empty<Action>();

                lock (_lock)
                {
                    if (_callbacks.Count > 0)
                    {
                        actions = _callbacks.ToArray();
                        _callbacks.Clear();
                    }
                }

                foreach (var action in actions) action.Invoke();
            }
        }
    }
}