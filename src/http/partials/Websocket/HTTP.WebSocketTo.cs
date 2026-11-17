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

            private readonly SemaphoreSlim _sendLocker = new SemaphoreSlim(1, 1);
            private readonly object _closeLocker = new object();
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
                    try
                    {
                        var ws = new ClientWebSocket();

                        foreach (var header in Headers) ws.Options.SetRequestHeader(header.Key, header.Value);

                        _socket._on.OnModify?.Invoke(null, ws);

                        await ws.ConnectAsync(host, CancellationToken.None);

                        MyServerRequest = new ServerRequest(ws, host, Headers);

                        MyUri = host;

                        _websocket = ws;

                        // connecting is done, the user can close (or open again) inside OnOpen
                        lock (_closeLocker)
                        {
                            _tryConnecting = false;
                        }

                        _socket._on.OnOpen?.Invoke(null, null);

                        _ReceiveData();
                    }
                    catch (Exception e)
                    {
                        Logger.Singleton.Submit(e);

                        try
                        {
                            await _websocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, string.Empty,
                                CancellationToken.None);
                        }
                        catch (Exception e2)
                        {
                            Logger.Singleton.Submit(e2);
                        }
                        finally
                        {
                            _websocket = null;
                        }

                        _socket._on.OnError?.Invoke(null, e);
                    }
                    finally
                    {
                        // REQUIRED: do not touch the closing flag, a close can already be running
                        lock (_closeLocker)
                        {
                            _tryConnecting = false;
                        }
                    }
                });
            }

            public Task Close()
            {
                return Close(WebSocketCloseStatus.NormalClosure);
            }

            public Task Close(WebSocketCloseStatus status)
            {
                System.Net.WebSockets.WebSocket socket;

                // the user and the receiver can close at the same time, only one of them goes on
                lock (_closeLocker)
                {
                    if (_tryClosing || _tryConnecting) return Task.CompletedTask;

                    socket = _isServerSide ? _websocketServerSide : _websocket;

                    if (socket == null) return Task.CompletedTask;

                    _tryClosing = true;
                }

                return Task.Run(async () =>
                {
                    try
                    {
                        var state = socket.State;

                        // close handshake only works in these states, otherwise it throws (WebSocketException 997)
                        if
                        (
                            state == WebSocketState.Open ||
                            state == WebSocketState.CloseReceived ||
                            state == WebSocketState.CloseSent
                        )
                        {
                            await socket.CloseAsync(status, string.Empty, CancellationToken.None);
                        }
                    }
                    catch (Exception e) when
                    (
                        e is WebSocketException ||
                        e is ObjectDisposedException ||
                        e is OperationCanceledException
                    )
                    {
                        // peer aborted or disposed the socket between the state check and the close
                    }
                    catch (Exception e)
                    {
                        Logger.Singleton.Submit(e);
                    }
                    finally
                    {
                        socket.Dispose();

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
                Send(messageType, buffer);
            }

            public void Data(string buffer, HTTP.MessageType messageType)
            {
                Send(messageType, buffer.GetBytes());
            }

            public void Data(string buffer, HTTP.MessageType messageType, Encoding encoding)
            {
                Send(messageType, buffer.GetBytes(encoding));
            }

            public void Event(string name, byte[] buffer, HTTP.MessageType messageType)
            {
                var header = Message.Create(name, buffer.LongLength);
                Send(messageType, header, buffer);
            }

            public void Event(string name, string buffer, HTTP.MessageType messageType)
            {
                var bytes = buffer.GetBytes();
                var header = Message.Create(name, bytes.LongLength);
                Send(messageType, header, bytes);
            }

            public void Event(string name, string buffer, HTTP.MessageType messageType, Encoding encoding)
            {
                var bytes = buffer.GetBytes(encoding);
                var header = Message.Create(name, bytes.LongLength);
                Send(messageType, header, bytes);
            }

            private async void Send(HTTP.MessageType type, params byte[][] buffers)
            {
                if (IsConnected() is false || buffers == null || buffers.Length <= 0) return;

                var messageType = type == MessageType.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary;

                // event header and body must be the same websocket message
                var messageContent = new ArraySegment<byte>(buffers.Length == 1 ? buffers[0] : Helper.ArrayConcat(buffers));

                try
                {
                    // websocket allows only one outstanding send
                    await _sendLocker.WaitAsync();

                    try
                    {
                        if (_isServerSide)
                            await _websocketServerSide.SendAsync(messageContent, messageType, true, CancellationToken.None);
                        else
                            await _websocket.SendAsync(messageContent, messageType, true, CancellationToken.None);
                    }
                    finally
                    {
                        _sendLocker.Release();
                    }
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                }
            }

            private void _ReceiveData()
            {
                // the loop awaits the socket, it does not hold a thread while the connection is idle
                ThreadPool.QueueUserWorkItem(ReceiveJob);
            }

            private async void ReceiveJob(object _)
            {
                var closeStatus = WebSocketCloseStatus.Empty;

                try
                {
                    const int size = 1024 * 8;
                    var buffer = new ArraySegment<byte>(new byte[size], 0, size);
                    var fragments = new System.IO.MemoryStream();

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

                        fragments.Write(buffer.Array, 0, result.Count);

                        if (fragments.Length > Framing.DefaultSize)
                            throw new InvalidOperationException($"{nameof(fragments)}: {fragments.Length}");

                        // wait the end of message
                        if (!result.EndOfMessage) continue;

                        var data = fragments.ToArray();

                        fragments.SetLength(0);

                        var messageType = result.MessageType == WebSocketMessageType.Text
                            ? MessageType.Text
                            : MessageType.Binary;

                        var stream = Helper.NewStream(data.LongLength);
                        await stream.WriteAsync(data, 0, data.Length);

                        if (Message.TryParse(stream, out var name, out var message, Helper.NewStream))
                        {
                            var reference = new byte[message.Length];
                            message.Position = 0;
                            await message.ReadAsync(reference, 0, reference.Length);
                            // Is Netly Event
                            _socket._on.OnEvent?.Invoke(null, (name, reference, messageType));
                        }
                        else
                            // Is Default buffer
                            _socket._on.OnData?.Invoke(null, (data, messageType));
                    }
                }
                catch (Exception e)
                {
                    Logger.Singleton.Submit(e);
                    closeStatus = WebSocketCloseStatus.EndpointUnavailable;
                }
                finally
                {
                    try
                    {
                        await Close(closeStatus);
                    }
                    catch (Exception e)
                    {
                        // async void: nothing can be thrown from here
                        Logger.Singleton.Submit(e);
                    }
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