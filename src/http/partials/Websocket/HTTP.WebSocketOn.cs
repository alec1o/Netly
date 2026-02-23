using System;
using System.Net.WebSockets;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class WebsocketOn : IHTTP.WebSocketOn
        {
            public EventHandler<WebSocketCloseStatus> OnClose { get; private set; }
            public EventHandler<(byte[] buffer, HTTP.MessageType messageType)> OnData { get; private set; }
            public EventHandler<Exception> OnError { get; private set; }
            public EventHandler<(string name, byte[] buffer, HTTP.MessageType messageType)> OnEvent { get; private set; }
            public EventHandler<ClientWebSocket> OnModify { get; private set; }
            public EventHandler OnOpen { get; private set; }

            public void Open(Action callback)
            {
                OnOpen += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke());
            }

            public void Error(Action<Exception> callback)
            {
                OnError += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke(@event));
            }

            public void Close(Action callback)
            {
                OnClose += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke());
            }

            public void Modify(Action<ClientWebSocket> callback)
            {
                OnModify += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke(@event));
            }

            public void Data(Action<byte[], MessageType> callback)
            {
                OnData += (@object, @event) =>
                    NDispatcher.Singleton.Submit(() => callback?.Invoke(@event.buffer, @event.messageType));
            }

            public void Event(Action<string, byte[], MessageType> callback)
            {
                OnEvent += (@object, @event) =>
                    NDispatcher.Singleton.Submit(() => callback?.Invoke(@event.name, @event.buffer, @event.messageType));
            }

            public void Close(Action<WebSocketCloseStatus> callback)
            {
                OnClose += (@object, @event) => NDispatcher.Singleton.Submit(() => callback?.Invoke(@event));
            }
        }
    }
}