using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Netly.Core;

namespace Netly
{
    public partial class HTTP
    {
        public partial class WebSocket
        {
            private class _To : ITo
            {
                public readonly Dictionary<string, string> m_headers = new Dictionary<string, string>();
                private readonly bool _isServerSide;
                private readonly WebSocket _socket;
                private bool _tryConnecting, _tryClosing, _initServerSide;
                private ClientWebSocket _websocket;
                private System.Net.WebSockets.WebSocket _websocketServerSide;
                public IRequest m_request;
                public Uri m_uri = new Uri("https://www.example.com");

                public _To(WebSocket socket)
                {
                    _socket = socket;
                    _tryConnecting = false;
                    _tryClosing = false;
                    _isServerSide = false;
                    m_request = null;
                }

                public _To(WebSocket socket, System.Net.WebSockets.WebSocket websocket, IRequest request)
                {
                    _socket = socket;
                    _isServerSide = true;
                    _websocketServerSide = websocket;
                    m_request = request;
                }

                public Task Open(Uri host)
                {
                    if (_socket.IsOpened || _tryConnecting || _tryClosing || _isServerSide) return Task.CompletedTask;

                    _tryConnecting = true;

                    return Task.Run(async () =>
                    {
                        try
                        {
                            var ws = new ClientWebSocket();

                            foreach (var header in m_headers) ws.Options.SetRequestHeader(header.Key, header.Value);

                            _socket._on.m_onModify?.Invoke(null, ws);

                            await ws.ConnectAsync(host, CancellationToken.None);

                            m_request = new Request(ws, host, m_headers);

                            m_uri = host;

                            _websocket = ws;

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

                public Task Close()
                {
                    return Close(WebSocketCloseStatus.NormalClosure);
                }

                public Task Close(WebSocketCloseStatus status)
                {
                    if (_tryClosing || _tryConnecting) return Task.CompletedTask;

                    if (_isServerSide)
                    {
                        if (_websocketServerSide == null) return Task.CompletedTask;
                    }
                    else
                    {
                        if (_websocket == null) return Task.CompletedTask;
                    }

                    _tryClosing = true;

                    return Task.Run(async () =>
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
                                    await _websocket.CloseAsync(status, string.Empty, CancellationToken.None);
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
                            if (_isServerSide)
                                _websocketServerSide = null;
                            else
                                _websocket = null;

                            _tryClosing = false;
                            _socket._on.m_onClose?.Invoke(null, status);
                        }
                    });
                }

                public void Data(byte[] buffer, bool isText)
                {
                    Send(buffer, isText);
                }

                public void Data(string buffer, bool isText)
                {
                    Send(NE.GetBytes(buffer, NE.DefaultEncoding), isText);
                }

                public void Event(string name, byte[] buffer)
                {
                    Send(EventManager.Create(name, buffer), false);
                }

                public void Event(string name, string buffer)
                {
                    Send(EventManager.Create(name, NE.GetBytes(buffer, NE.Encoding.UTF8)), false);
                }

                private void Send(byte[] buffer, bool isText)
                {
                    if (IsConnected() is false || buffer == null || buffer.Length <= 0) return;

                    var messageContent = new ArraySegment<byte>(buffer);
                    var messageType = isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
                    // Is Always true because our send all buffer on same moment is internal
                    // behaviour that will parse the data and put EndOfMessage=true when send last fragment of buffer
                    var endOfMessage = true;

                    if (_isServerSide)
                        _websocketServerSide.SendAsync
                        (
                            messageContent,
                            messageType,
                            endOfMessage,
                            CancellationToken.None
                        );
                    else
                        _websocket.SendAsync
                        (
                            messageContent,
                            messageType,
                            endOfMessage,
                            CancellationToken.None
                        );
                }

                private void _ReceiveData()
                {
                    if (_isServerSide)
                    {
                        // optimize server resources.
                        ThreadPool.QueueUserWorkItem(ReceiveJob);
                    }
                    else
                    {
                        // dedicate thread
                        new Thread(ReceiveJob) { IsBackground = true }.Start();
                    }
                }

                private async void ReceiveJob(object _)
                {
                    var closeStatus = WebSocketCloseStatus.Empty;

                    try
                    {
                        const int size = 1024 * 8;
                        var buffer = new ArraySegment<byte>(new byte[size], 0, size);

                        while (_socket.IsOpened)
                        {
                            var result = _isServerSide
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
                                // Is Netly Event
                                _socket._on.m_onEvent?.Invoke(null, (eventData.name, eventData.data));
                            else
                                // Is Default buffer
                                _socket._on.m_onData?.Invoke(null,
                                    (data, result.MessageType == WebSocketMessageType.Text));
                        }
                    }
                    catch
                    {
                        closeStatus = WebSocketCloseStatus.EndpointUnavailable;
                    }
                    finally
                    {
                        await Close(closeStatus);
                    }
                }

                public bool IsConnected()
                {
                    if (_isServerSide)
                        return _websocketServerSide != null && _websocketServerSide.State == WebSocketState.Open;
                    return _websocket != null && _websocket.State == WebSocketState.Open;
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