using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byter;
using Netly.Interfaces;

namespace Netly
{
    public partial class HTTP
    {
        internal class WebsocketTo : IHTTP.WebSocketTo
        {
            /// <summary>
            ///     Prevents port collision during rapid WebSocket client initialization
            /// </summary>
            private static readonly object CONNECT_LOCKER = new object();

            private readonly bool _isServerSide;
            private readonly WebSocket _socket;
            public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
            private bool _tryConnecting, _tryClosing, _initServerSide;
            private ClientWebSocket _websocket;
            private System.Net.WebSockets.WebSocket _websocketServerSide;
            private const string ENCRYPTED_SCHEME = "wss";
            private const string UNENCRYPTED_SCHEME = "ws";
            public Uri MyUri = new Uri($"{ENCRYPTED_SCHEME}://{IPAddress.Any}");

            public WebsocketTo(WebSocket socket)
            {
                _socket = socket;
                _tryConnecting = false;
                _tryClosing = false;
                _isServerSide = false;
                MyServerRequest = null;
            }

            public WebsocketTo(WebSocket socket, System.Net.WebSockets.WebSocket websocket,
                IHTTP.ServerRequest myServerRequest)
            {
                _socket = socket;
                _isServerSide = true;
                _websocketServerSide = websocket;
                MyServerRequest = myServerRequest;
                MyUri = new Uri
                (
                    $"{(myServerRequest.IsEncrypted ? ENCRYPTED_SCHEME : UNENCRYPTED_SCHEME)}://{myServerRequest.RemoteEndPoint.Address}:{myServerRequest.RemoteEndPoint.Port}"
                );
            }

            public IHTTP.ServerRequest MyServerRequest { get; private set; }

            public Task Open(Uri host)
            {
                if (_socket.IsOpened || _tryConnecting || _tryClosing || _isServerSide) return Task.CompletedTask;

                _tryConnecting = true;

                return Task.Run(async () =>
                {
                    Thread.Sleep(500);

                    try
                    {
                        ClientWebSocket ws;

                        lock (CONNECT_LOCKER)
                        {
                            // Why is this lock necessary?
                            // When creating multiple WebSocket clients in quick succession, the operating system may reuse the same 
                            // local port for different connections before fully releasing the previous ones. This happens because 
                            // ephemeral port allocation is fast but not always synchronized optimally for high-concurrency scenarios.
                            //
                            // By using this global lock (via CONNECT_LOCKER) and introducing a small delay, we allow the OS enough 
                            // time to properly assign unique local ports for each connection. This effectively prevents port reuse 
                            // issues and connection instability under heavy load.
                            Thread.Sleep(TimeSpan.FromMilliseconds(byte.MaxValue)); // (Only client side)
                            ws = new ClientWebSocket();
                        }
                        
                        foreach (var header in Headers) ws.Options.SetRequestHeader(header.Key, header.Value);

                        _socket._on.OnModify?.Invoke(null, ws);

                        await ws.ConnectAsync(host, CancellationToken.None);

                        MyServerRequest = new ServerRequest(ws, host, Headers);

                        MyUri = host;

                        _websocket = ws;

                        _socket._on.OnOpen?.Invoke(null, null);

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

                        _socket._on.OnError?.Invoke(null, e);
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
                        _socket._on.OnClose?.Invoke(null, status);
                    }
                });
            }

            public void Data(byte[] buffer, HTTP.MessageType messageType)
            {
                Send(buffer, messageType);
            }

            public void Data(string buffer, HTTP.MessageType messageType)
            {
                Send(buffer.GetBytes(), messageType);
            }

            public void Data(string buffer, HTTP.MessageType messageType, Encoding encoding)
            {
                Send(buffer.GetBytes(encoding), messageType);
            }

            public void Event(string name, byte[] buffer, HTTP.MessageType messageType)
            {
                Send(NetlyEnvironment.EventManager.Create(name, buffer), messageType);
            }

            public void Event(string name, string buffer, HTTP.MessageType messageType)
            {
                Send(NetlyEnvironment.EventManager.Create(name, buffer.GetBytes()), messageType);
            }

            public void Event(string name, string buffer, HTTP.MessageType messageType, Encoding encoding)
            {
                Send(NetlyEnvironment.EventManager.Create(name, buffer.GetBytes(encoding)), messageType);
            }

            private void Send(byte[] buffer, HTTP.MessageType type)
            {
                if (IsConnected() is false || buffer == null || buffer.Length <= 0) return;

                var messageContent = new ArraySegment<byte>(buffer);
                var messageType = type == MessageType.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
                // Is Always true because our send all buffer on same moment is internal
                // behaviour that will parse the data and put EndOfMessage=true when send last fragment of buffer
                const bool endOfMessage = true;

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
                    // optimize server resources.
                    ThreadPool.QueueUserWorkItem(ReceiveJob);
                else
                    // dedicate thread
                    new Thread(ReceiveJob) { IsBackground = true }.Start();
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

                        var eventData = NetlyEnvironment.EventManager.Verify(data);

                        var messageType = result.MessageType == WebSocketMessageType.Text
                            ? MessageType.Text
                            : MessageType.Binary;

                        if (eventData.data != null && eventData.name != null)
                            // Is Netly Event
                            _socket._on.OnEvent?.Invoke(null, (eventData.name, eventData.data, messageType));
                        else
                            // Is Default buffer
                            _socket._on.OnData?.Invoke(null, (data, messageType));
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

                _socket._on.OnModify?.Invoke(null, _websocket);
                _socket._on.OnOpen?.Invoke(null, null);
            }
        }
    }
}