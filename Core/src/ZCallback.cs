using System;
using System.Collections.Generic;

namespace Zenet.Core
{
    public static class ZCallback
    {
        private static List<Action> Callbacks = new List<Action>();
        public static bool Manual;

        public static void Execute(Action callback)
        {
            if (Manual)
            {
                Callbacks.Add(callback);
            }
            else
            {
                callback?.Invoke();
            }
        }

        public static void Update()
        {
            try
            {
                if (Manual && Callbacks.Count > 0)
                {
                    foreach (Action callback in Callbacks.ToArray())
                    {
                        callback?.Invoke();
                        Callbacks.Remove(callback);
                    }
                }
            }
            catch
            {

            }
        }
    }
}
