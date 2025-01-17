---
title: Interface IRUDP.ClientTo
sidebar_label: IRUDP.ClientTo
description: "RUDP Client actions container (interface)"
---
# Interface IRUDP.ClientTo
RUDP Client actions container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L11)
```csharp title="Declaration"
public interface IRUDP.ClientTo
```
## Methods
### Open(Host)
Use to open connection (if disconnected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L18)
```csharp title="Declaration"
Task Open(Host host)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *host* | Host (remote endpoint) |

### Close()
Use to close connection (if connected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L24)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### Data(byte[], MessageType)
Send raw data
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L31)
```csharp title="Declaration"
void Data(byte[] data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *data* | data - bytes |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### Data(string, MessageType)
Send raw data
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L38)
```csharp title="Declaration"
void Data(string data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### Data(string, MessageType, Encoding)
Send raw data
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L46)
```csharp title="Declaration"
void Data(string data, RUDP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |
| `System.Text.Encoding` | *encoding* | Data encoding method |

### Event(string, byte[], MessageType)
Send event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L55)
```csharp title="Declaration"
void Event(string name, byte[] data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.Byte[]` | *data* | Event data - bytes |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### Event(string, string, MessageType)
Send event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L63)
```csharp title="Declaration"
void Event(string name, string data, RUDP.MessageType messageType)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |

### Event(string, string, MessageType, Encoding)
Send event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientTo.cs#L72)
```csharp title="Declaration"
void Event(string name, string data, RUDP.MessageType messageType, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| [Netly.RUDP.MessageType](../Netly/RUDP.MessageType) | *messageType* | message type |
| `System.Text.Encoding` | *encoding* | Event data encoding method |

