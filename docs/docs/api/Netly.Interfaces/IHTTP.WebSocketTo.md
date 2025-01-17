---
title: Interface IHTTP.WebSocketTo
sidebar_label: IHTTP.WebSocketTo
---
# Interface IHTTP.WebSocketTo


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L10)
```csharp title="Declaration"
public interface IHTTP.WebSocketTo
```
## Methods
### Open(Uri)
Open Client Connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L16)
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
Close Client Connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L21)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### Close(WebSocketCloseStatus)
Close Client Connection (with Close Status)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L27)
```csharp title="Declaration"
Task Close(WebSocketCloseStatus closeStatus)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Net.WebSockets.WebSocketCloseStatus` | *closeStatus* | Close Status |

### Data(byte[], MessageType)
Send data for server (bytes)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L34)
```csharp title="Declaration"
void Data(byte[] buffer, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *buffer* | Data buffer |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* | Message Type (Binary|Text) |

### Data(string, MessageType)
Send data for server (string)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L41)
```csharp title="Declaration"
void Data(string buffer, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *buffer* | Data buffer |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* | Message Type (Binary|Text) |

### Data(string, MessageType, Encoding)
Send data for server (string)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L49)
```csharp title="Declaration"
void Data(string buffer, HTTP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *buffer* | Data buffer |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* | Message Type (Binary|Text) |
| `System.Text.Encoding` | *encoding* | String encoding |

### Event(string, byte[], MessageType)
Send Netly event for server (bytes)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L56)
```csharp title="Declaration"
void Event(string name, byte[] buffer, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.Byte[]` | *buffer* | Event buffer |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |  |

### Event(string, string, MessageType)
Send Netly event for server (string)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L63)
```csharp title="Declaration"
void Event(string name, string buffer, HTTP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *buffer* | Event buffer |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |  |

### Event(string, string, MessageType, Encoding)
Send Netly event for server (string)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketTo.cs#L71)
```csharp title="Declaration"
void Event(string name, string buffer, HTTP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *buffer* | Event buffer |
| [Netly.HTTP.MessageType](../Netly/HTTP.MessageType) | *messageType* |  |
| `System.Text.Encoding` | *encoding* | String encoding |

