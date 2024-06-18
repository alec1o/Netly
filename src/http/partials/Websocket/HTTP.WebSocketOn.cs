using System;
using System.Net.WebSockets;
using Netly.Interfaces;
using Env = Netly.NetlyEnvironment;

namespace Netly
{
    public partial class HTTP
    {
        internal class WebsocketOn : IHTTP.WebSocketOn
        {
            public EventHandler<WebSocketCloseStatus> OnClose { get; private set; }
            public EventHandler<(byte[] buffer, bool isText)> OnData { get; private set; }
            public EventHandler<Exception> OnError { get; private set; }
            public EventHandler<(string name, byte[] buffer)> OnEvent { get; private set; }
            public EventHandler<ClientWebSocket> OnModify { get; private set; }
            public EventHandler OnOpen { get; private set; }

            public void Open(Action callback)
            {
                OnOpen += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
            }

            public void Error(Action<Exception> callback)
            {
                OnError += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
            }

            public void Close(Action callback)
            {
                OnClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke());
            }

            public void Modify(Action<ClientWebSocket> callback)
            {
                OnModify += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
            }

            public void Data(Action<byte[], bool> callback)
            {
                OnData += (@object, @event) =>
                    Env.MainThread.Add(() => callback?.Invoke(@event.buffer, @event.isText));
            }

            public void Event(Action<string, byte[]> callback)
            {
                OnEvent += (@object, @event) =>
                    Env.MainThread.Add(() => callback?.Invoke(@event.name, @event.buffer));
            }

            public void Close(Action<WebSocketCloseStatus> callback)
            {
                OnClose += (@object, @event) => Env.MainThread.Add(() => callback?.Invoke(@event));
            }
        }
    }
}