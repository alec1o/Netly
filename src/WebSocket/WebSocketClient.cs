﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using Netly.Core;

namespace Netly
{
    public class WebSocketClient : IWebsocketClient
    {
        public bool IsOpened => _websocket != null && _websocket.State == WebSocketState.Open;
        public Uri Uri { get; internal set; }
        public Headers Headers { get; internal set; }
        public Cookie[] Cookies { get; internal set; }

        private EventHandler<(string name, byte[] buffer, WebSocketMessageType type)> _onEvent;
        private EventHandler<WebSocketCloseStatus> _onClose;
        private EventHandler<ClientWebSocket> _onModify;
        private EventHandler<Exception> _onError;
        private ClientWebSocket _websocket;
        public WebSocketClient()
        {
        }


        public void Open(Uri uri)
        {
        }


        private void _ReceiveData()
        {
            }


            async void InternalReceiveTask(object _)
            {
        }


        public void Close()
        {
        }


        public void Close(WebSocketCloseStatus status)
        {
        }

        public void ToData(byte[] buffer, BufferType bufferType = BufferType.Binary)
        {
        }

        public void ToData(string buffer, BufferType bufferType = BufferType.Text)
        }
        public void ToEvent(string name, byte[] buffer)
        {
        }

        public void ToEvent(string name, string buffer)
        {
        }

        public void OnOpen(Action callback)
        {
        }

        public void OnClose(Action<WebSocketCloseStatus> callback)
        {
        }

        public void OnClose(Action callback)
        {
            _onClose += (_, __) =>
            {
                // Run Task on custom thread
                MainThread.Add(() => callback?.Invoke());
            };
        }

        public void OnError(Action<Exception> callback)
        {
            _onError += (_, exception) =>
            {
                // Run Task on custom thread
                MainThread.Add(() => callback?.Invoke(exception));
            };
        }

        public void OnData(Action<byte[], BufferType> callback)
        {
        }

        public void OnEvent(Action<string, byte[], BufferType> callback)
        {
            _onEvent += (_, container) =>
            {
                // Run Task on custom thread
                MainThread.Add(() => callback?.Invoke(container.name, container.buffer,
                    BufferTypeWrapper.FromWebsocketMessageType(container.type)));
            };
        }

        public void OnModify(Action<ClientWebSocket> callback)
        {
            _onModify += (_, ws) =>
            {
                // Run Task on custom thread
                MainThread.Add(() => callback?.Invoke(ws));
            };
        }
    }
}