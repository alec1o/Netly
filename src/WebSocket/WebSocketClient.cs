using System;
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

        private CancellationToken CancellationToken => CancellationToken.None;
        private EventHandler<(string name, byte[] buffer, WebSocketMessageType type)> _onEvent;
        private EventHandler<(byte[] buffer, WebSocketMessageType type)> _onData;
        private EventHandler<WebSocketCloseStatus> _onClose;
        private EventHandler<ClientWebSocket> _onModify;
        private EventHandler<Exception> _onError;
        private EventHandler _onOpen;
        private readonly List<(byte[] buffer, BufferType bufferType)> _bufferList;
        private bool _tryConnecting, _tryClosing;
        private readonly object _bufferLock;
        private ClientWebSocket _websocket;


        public WebSocketClient()
        {
            _bufferList = new List<(byte[] buffer, BufferType bufferType)>();
            _bufferLock = new object();
            _tryConnecting = false;
            _tryClosing = false;
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
            Close(WebSocketCloseStatus.Empty);
        }


        public void Close(WebSocketCloseStatus status)
        {
            if (!IsOpened || _tryClosing || _tryConnecting) return;
            _tryClosing = true;

            ThreadPool.QueueUserWorkItem(InternalTask);

            async void InternalTask(object _)
            {
                try
                {
                    await _websocket.CloseAsync(status, String.Empty, CancellationToken);
                    _websocket.Dispose();
                }
                catch (Exception e)
                {
                    // TODO: FIX IT
                    Console.WriteLine(e);
                }
                finally
                {
                    lock (_bufferLock)
                    {
                        _bufferList.Clear();
                    }

                    _websocket = null;
                    _tryClosing = false;
                    _onClose(null, status);
                }
            }
        }

        public void ToData(byte[] buffer, BufferType bufferType = BufferType.Binary)
        {
            if (IsOpened)
            {
                lock (_bufferLock)
                {
                    _bufferList.Add((buffer, bufferType));
                }
            }
        }

        public void ToData(string buffer, BufferType bufferType = BufferType.Text)
        {
            ToData(NE.GetBytes(buffer, NE.Default), bufferType);
        }

        public void ToEvent(string name, byte[] buffer)
        {
            ToData(EventManager.Create(name, buffer), BufferType.Binary);
        }

        public void ToEvent(string name, string buffer)
        {
            ToEvent(name, NE.GetBytes(buffer, NE.Mode.UTF8));
        }

        public void OnOpen(Action callback)
        {
            _onOpen += (_, __) =>
            {
                // Run Task on custom thread
                MainThread.Add(() => callback?.Invoke());
            };
        }

        public void OnClose(Action<WebSocketCloseStatus> callback)
        {
            _onClose += (_, status) =>
            {
                // Run Task on custom thread
                MainThread.Add(() => callback?.Invoke(status));
            };
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
            _onData += (_, container) =>
            {
                // Run Task on custom thread
                MainThread.Add(() =>
                    callback?.Invoke(container.buffer, BufferTypeWrapper.FromWebsocketMessageType(container.type)));
            };
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