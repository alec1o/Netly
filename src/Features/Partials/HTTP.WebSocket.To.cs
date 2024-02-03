using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using Netly.Core;
using Netly.Interfaces;

namespace Netly.Features
{
    public partial class HTTP
    {
        public partial class WebSocket
        {
            private class _To : Interfaces.HTTP.WebSocket.ITo
            {
                public Interfaces.HTTP.IRequest m_request;
                private readonly WebSocket _socket;
                public Uri m_uri = new Uri("https://www.example.com");
                private readonly bool _isServerSide;
                private bool _tryConnecting, _tryClosing, _initServerSide;
                private ClientWebSocket _websocket;
                private System.Net.WebSockets.WebSocket _websocketServerSide;
                private readonly object _bufferLock = new object();

                private readonly List<(byte[] buffer, bool isText)> _bufferList = new List<(byte[], bool )>();

                public _To(WebSocket socket)
                {
                    _socket = socket;
                    _tryConnecting = false;
                    _tryClosing = false;
                    _isServerSide = false;
                    this.m_request = null;
                }

                public _To(WebSocket socket, System.Net.WebSockets.WebSocket websocket, Interfaces.HTTP.IRequest request)
                {
                    _socket = socket;
                    _isServerSide = true;
                    _websocketServerSide = websocket;
                    this.m_request = request;
                }

                public void Open(Uri host)
                {
                    if (_socket.IsOpened || _tryConnecting || _tryClosing || _isServerSide) return;

                    _tryConnecting = true;

                    ThreadPool.QueueUserWorkItem(async (@object) =>
                    {
                        try
                        {
                            var ws = new ClientWebSocket();
                            _socket._on.m_onModify?.Invoke(null, ws);
                            await ws.ConnectAsync(host, CancellationToken.None);
                            m_request = new Request(ws, host);
                            _websocket = ws;
                            m_uri = host;
                            _socket._on.m_onOpen?.Invoke(null, null);
                            _ReceiveData();
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                await _websocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, string.Empty,
                                    CancellationToken.None);
                            }
                            catch
                            {
                                // ignored
                            }
                            finally
                            {
                                _websocket = null;
                            }

                            _socket._on.m_onError?.Invoke(null, e);
                        }
                        finally
                        {
                            _tryClosing = false;
                            _tryConnecting = false;
                        }
                    });
                }

                public void Close()
                {
                    Close(WebSocketCloseStatus.NormalClosure);
                }

                public void Close(WebSocketCloseStatus status)
                {
                    if (_tryClosing || _tryConnecting) return;

                    if (_isServerSide)
                    {
                        if (_websocketServerSide == null) return;
                    }
                    else
                    {
                        if (_websocket == null) return;
                    }

                    _tryClosing = true;

                    ThreadPool.QueueUserWorkItem(InternalTask);

                    async void InternalTask(object _)
                    {
                        try
                        {
                            var state = _initServerSide ? _websocketServerSide.State : _websocket.State;

                            /*
                                 System.Net.WebSockets.WebSocketException (997):
                                 The WebSocket is in an invalid state ('Aborted') for this operation.
                                 Valid states are: 'Open, CloseReceived, CloseSent'
                            */
                            if
                            (
                                state == WebSocketState.Open ||
                                state == WebSocketState.CloseReceived ||
                                state == WebSocketState.CloseSent
                            )
                            {
                                if (_isServerSide)
                                {
                                    await _websocketServerSide.CloseAsync(status, string.Empty, CancellationToken.None);
                                    _websocketServerSide.Dispose();
                                }
                                else
                                {
                                    await _websocket.CloseAsync(status, String.Empty, CancellationToken.None);
                                    _websocket.Dispose();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // HACK: FIX IT
                            Console.WriteLine(e);
                        }
                        finally
                        {
                            lock (_bufferLock)
                            {
                                _bufferList.Clear();
                            }

                            if (_isServerSide)
                            {
                                _websocketServerSide = null;
                            }
                            else
                            {
                                _websocket = null;
                            }

                            _tryClosing = false;
                            _socket._on.m_onClose(null, status);
                        }
                    }
                }

                public void Data(byte[] buffer, bool isText)
                {
                    if (IsConnected())
                    {
                        lock (_bufferLock)
                        {
                            _bufferList.Add((buffer, isText));
                        }
                    }
                }

                public void Data(string buffer, bool isText)
                {
                    Data(NE.GetBytes(buffer, NE.Default), isText);
                }

                public void Event(string name, byte[] buffer)
                {
                    Data(EventManager.Create(name, buffer), false);
                }

                public void Event(string name, string buffer)
                {
                    Event(name, NE.GetBytes(buffer, NE.Encoding.UTF8));
                }

                private void _ReceiveData()
                {
                    ThreadPool.QueueUserWorkItem(InternalReceiveTask);

                    async void InternalSendTask(object _)
                    {
                        while (IsConnected())
                        {
                            try
                            {
                                // ReSharper disable once InconsistentlySynchronizedField
                                // ^^^ Because if check before will prevent lock target object to just check if is empty
                                // And just lock object when detected that might have any buffer to send
                                if (_bufferList.Count > 0)
                                {
                                    bool success = false;
                                    WebSocketMessageType messageType = WebSocketMessageType.Close;

                                    // Is Always true because our send all buffer on same moment is internal
                                    // behaviour that will parse the data and put EndOfMessage=true when send last fragment of buffer
                                    const bool endOfMessage = true;

                                    byte[] buffer = null;

                                    lock (_bufferLock)
                                    {
                                        if (_bufferList.Count > 0)
                                        {
                                            messageType = _bufferList[0].isText
                                                ? WebSocketMessageType.Text
                                                : WebSocketMessageType.Binary;
                                            buffer = _bufferList[0].buffer;
                                            success = true;

                                            _bufferList.RemoveAt(0);
                                        }
                                    }

                                    if (success)
                                    {
                                        var bufferToSend = new ArraySegment<byte>(buffer);

                                        if (_isServerSide)
                                        {
                                            await _websocketServerSide.SendAsync
                                            (
                                                bufferToSend,
                                                messageType,
                                                endOfMessage,
                                                CancellationToken.None
                                            );
                                        }
                                        else
                                        {
                                            await _websocket.SendAsync
                                            (
                                                bufferToSend,
                                                messageType,
                                                endOfMessage,
                                                CancellationToken.None
                                            );
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }


                    async void InternalReceiveTask(object _)
                    {
                        WebSocketCloseStatus closeStatus = WebSocketCloseStatus.Empty;

                        try
                        {
                            ThreadPool.QueueUserWorkItem(InternalSendTask);

                            const int size = 1024 * 8;
                            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[size], 0, size);

                            while (_socket.IsOpened)
                            {
                                WebSocketReceiveResult result = _isServerSide
                                    ? await _websocketServerSide.ReceiveAsync(buffer, CancellationToken.None)
                                    : await _websocket.ReceiveAsync(buffer, CancellationToken.None);

                                if (result.MessageType == WebSocketMessageType.Close || buffer.Array == null)
                                {
                                    closeStatus = result.CloseStatus ?? closeStatus;
                                    break;
                                }

                                var data = new byte[result.Count];

                                Array.Copy(buffer.Array, 0, data, 0, data.Length);

                                var eventData = EventManager.Verify(data);

                                if (eventData.data != null && eventData.name != null)
                                {
                                    // Is Netly Event
                                    _socket._on.m_onEvent?.Invoke(null, (eventData.name, eventData.data));
                                }
                                else
                                {
                                    // Is Regular Data
                                    _socket._on.m_onData?.Invoke(null,
                                        (data, result.MessageType == WebSocketMessageType.Text));
                                }
                            }
                        }
                        catch
                        {
                            closeStatus = WebSocketCloseStatus.EndpointUnavailable;
                        }
                        finally
                        {
                            Close(closeStatus);
                        }
                    }
                }

                public bool IsConnected()
                {
                    if (_isServerSide)
                    {
                        return _websocketServerSide != null && _websocketServerSide.State == WebSocketState.Open;
                    }
                    else
                    {
                        return _websocket != null && _websocket.State == WebSocketState.Open;
                    }
                }

                public void InitWebSocketServerSide()
                {
                    if (_initServerSide) return;
                    _ReceiveData();
                    _initServerSide = true;
                }
            }
        }
    }
}