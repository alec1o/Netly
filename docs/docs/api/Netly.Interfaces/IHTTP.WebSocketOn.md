---
title: Interface IHTTP.WebSocketOn
sidebar_label: IHTTP.WebSocketOn
---
# Interface IHTTP.WebSocketOn


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketOn.cs#L8)
```csharp title="Declaration"
public interface IHTTP.WebSocketOn : IOn<ClientWebSocket>
```
## Methods
### Data(Action&lt;byte[], MessageType&gt;)
Handle data received
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketOn.cs#L14)
```csharp title="Declaration"
void Data(Action<byte[], HTTP.MessageType> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Byte[],Netly.HTTP.MessageType>` | *callback* | Callback |

### Event(Action&lt;string, byte[], MessageType&gt;)
Handle (netly event) received
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketOn.cs#L20)
```csharp title="Declaration"
void Event(Action<string, byte[], HTTP.MessageType> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.String,System.Byte[],Netly.HTTP.MessageType>` | *callback* | Callback |

### Close(Action&lt;WebSocketCloseStatus&gt;)
Handle connection closed
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocketOn.cs#L26)
```csharp title="Declaration"
void Close(Action<WebSocketCloseStatus> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Net.WebSockets.WebSocketCloseStatus>` | *callback* | Callback |

