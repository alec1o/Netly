using System;

namespace Zenet.Core
{
    public static class ZAsync
    {       
        public static void Execute(Action callback)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((_) => callback?.Invoke());
        }
    }
}
