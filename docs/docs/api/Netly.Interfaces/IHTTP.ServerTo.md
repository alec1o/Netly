---
title: Interface IHTTP.ServerTo
sidebar_label: IHTTP.ServerTo
description: "HTTP.Server action creator container"
---
# Interface IHTTP.ServerTo
HTTP.Server action creator container

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L12)
```csharp title="Declaration"
public interface IHTTP.ServerTo
```
## Methods
### Open(Uri)
Open Server Connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L18)
```csharp title="Declaration"
Task Open(Uri host)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Uri` | *host* | Server Uri |

### Close()
Close Server Connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L24)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### WebsocketDataBroadcast(byte[], MessageType)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L26)
```csharp title="Declaration"
void WebsocketDataBroadcast(byte[] data, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *data* |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |

### WebsocketDataBroadcast(string, MessageType)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L27)
```csharp title="Declaration"
void WebsocketDataBroadcast(string data, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *data* |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |

### WebsocketDataBroadcast(string, MessageType, Encoding)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L28)
```csharp title="Declaration"
void WebsocketDataBroadcast(string data, HTTP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *data* |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |
| `System.Text.Encoding` | *encoding* |

### WebsocketEventBroadcast(string, byte[], MessageType)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L30)
```csharp title="Declaration"
void WebsocketEventBroadcast(string name, byte[] data, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.Byte[]` | *data* |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |

### WebsocketEventBroadcast(string, string, MessageType)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L31)
```csharp title="Declaration"
void WebsocketEventBroadcast(string name, string data, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *data* |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |

### WebsocketEventBroadcast(string, string, MessageType, Encoding)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerTo.cs#L32)
```csharp title="Declaration"
void WebsocketEventBroadcast(string name, string data, HTTP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *data* |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |
| `System.Text.Encoding` | *encoding* |

