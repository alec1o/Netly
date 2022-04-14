﻿using System;
using System.Collections.Generic;

namespace Zenet.Network
{
    public static class Callback
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
            if (!Manual || Callbacks.Count <= 0) return;

            foreach(var callback in Callbacks.ToArray())
            {
                callback?.Invoke();
                Callbacks.Remove(callback);
            }
        }
    }
}