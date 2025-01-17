---
title: Interface IRUDP.ServerTo
sidebar_label: IRUDP.ServerTo
description: "RUDP Server actions container (interface)"
---
# Interface IRUDP.ServerTo
RUDP Server actions container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L11)
```csharp title="Declaration"
public interface IRUDP.ServerTo
```
## Methods
### Open(Host)
Use to open listening connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L18)
```csharp title="Declaration"
Task Open(Host host)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *host* | Host (local endpoint) |

### Close()
Use to close listening connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L24)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### DataBroadcast(byte[], MessageType)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L31)
```csharp title="Declaration"
void DataBroadcast(byte[] data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *data* | data - bytes |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### DataBroadcast(string, MessageType)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L38)
```csharp title="Declaration"
void DataBroadcast(string data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### DataBroadcast(string, MessageType, Encoding)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L46)
```csharp title="Declaration"
void DataBroadcast(string data, RUDP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |
| `System.Text.Encoding` | *encoding* | Data encoding method |

### EventBroadcast(string, byte[], MessageType)
Use to send event (netly event) to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L54)
```csharp title="Declaration"
void EventBroadcast(string name, byte[] data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.Byte[]` | *data* | Event data - bytes |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### EventBroadcast(string, string, MessageType)
Use to send event (netly event) to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L62)
```csharp title="Declaration"
void EventBroadcast(string name, string data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### EventBroadcast(string, string, MessageType, Encoding)
Use to send event (netly event) to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerTo.cs#L71)
```csharp title="Declaration"
void EventBroadcast(string name, string data, RUDP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |
| `System.Text.Encoding` | *encoding* | Event data encoding method |

