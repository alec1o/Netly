using System;

namespace Zenet.Network
{
    public static class Async
    {       
        public static void Thread(Action callback)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((_) => callback?.Invoke());
        }
    }
}
