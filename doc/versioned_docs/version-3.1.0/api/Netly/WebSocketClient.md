---
title: Class WebSocketClient
sidebar_label: WebSocketClient
---
# Class WebSocketClient


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L10)
```csharp title="Declaration"
public class WebSocketClient
```
## Properties
### IsOpened

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L12)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### Uri

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L13)
```csharp title="Declaration"
public Uri Uri { get; }
```
### Headers

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L14)
```csharp title="Declaration"
public KeyValueContainer Headers { get; }
```
### Cookies

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L15)
```csharp title="Declaration"
public Cookie[] Cookies { get; }
```
## Methods
### Open(Uri)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L63)
```csharp title="Declaration"
public void Open(Uri uri)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Uri` | *uri* |

### Close()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L249)
```csharp title="Declaration"
public void Close()
```
### Close(WebSocketCloseStatus)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L255)
```csharp title="Declaration"
public void Close(WebSocketCloseStatus status)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Net.WebSockets.WebSocketCloseStatus` | *status* |

### ToData(byte[], BufferType)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L314)
```csharp title="Declaration"
public void ToData(byte[] buffer, BufferType bufferType = BufferType.Binary)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *buffer* |
| [Netly.Core.BufferType](../Netly.Core/BufferType) | *bufferType* |

### ToData(string, BufferType)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L325)
```csharp title="Declaration"
public void ToData(string buffer, BufferType bufferType = BufferType.Text)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *buffer* |
| [Netly.Core.BufferType](../Netly.Core/BufferType) | *bufferType* |

### ToEvent(string, byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L330)
```csharp title="Declaration"
public void ToEvent(string name, byte[] buffer)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.Byte[]` | *buffer* |

### ToEvent(string, string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L335)
```csharp title="Declaration"
public void ToEvent(string name, string buffer)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *buffer* |

### OnOpen(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L340)
```csharp title="Declaration"
public void OnOpen(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnClose(Action&lt;WebSocketCloseStatus&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L349)
```csharp title="Declaration"
public void OnClose(Action<WebSocketCloseStatus> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.WebSockets.WebSocketCloseStatus>` | *callback* |

### OnClose(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L358)
```csharp title="Declaration"
public void OnClose(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnError(Action&lt;Exception&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L367)
```csharp title="Declaration"
public void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Exception>` | *callback* |

### OnData(Action&lt;byte[], BufferType&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L376)
```csharp title="Declaration"
public void OnData(Action<byte[], BufferType> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Byte[],Netly.Core.BufferType>` | *callback* |

### OnEvent(Action&lt;string, byte[], BufferType&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L386)
```csharp title="Declaration"
public void OnEvent(Action<string, byte[], BufferType> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.String,System.Byte[],Netly.Core.BufferType>` | *callback* |

### OnModify(Action&lt;ClientWebSocket&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/WebSocket/WebSocketClient.cs#L396)
```csharp title="Declaration"
public void OnModify(Action<ClientWebSocket> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.WebSockets.ClientWebSocket>` | *callback* |

