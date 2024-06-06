using System;

namespace Netly
{
    public partial class NetlyEnvironment
    {
        private class MyLogger : ILogger
        {
            private EventHandler<Exception> _errorEvent;
            private EventHandler<string> _messageEvent;

            public void Create(string message)
            {
                _messageEvent?.Invoke(null, message);
            }

            public void Create(Exception exception)
            {
                _errorEvent?.Invoke(null, exception);
            }

            public void On(Action<string> callback, bool useMainThread = false)
            {
                _messageEvent += (e, value) => Invoke(() => callback?.Invoke(value), useMainThread);
            }

            public void On(Action<Exception> callback, bool useMainThread = false)
            {
                _errorEvent += (e, value) => Invoke(() => callback?.Invoke(value), useMainThread);
            }

            private void Invoke(Action action, bool isMainThread)
            {
                if (isMainThread)
                    MainThread.Add(action);
                else
                    action?.Invoke();
            }
        }
    }
}