---
title: Interface IUDP.ServerTo
sidebar_label: IUDP.ServerTo
description: "UDP Server actions container (interface)"
---
# Interface IUDP.ServerTo
UDP Server actions container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L11)
```csharp title="Declaration"
public interface IUDP.ServerTo
```
## Methods
### Open(Host)
Use to open listening connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L18)
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
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L24)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### DataBroadcast(byte[])
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L30)
```csharp title="Declaration"
void DataBroadcast(byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *data* | data - bytes |

### DataBroadcast(string)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L36)
```csharp title="Declaration"
void DataBroadcast(string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |

### DataBroadcast(string, Encoding)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L43)
```csharp title="Declaration"
void DataBroadcast(string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |
| `System.Text.Encoding` | *encoding* | Data encoding method |

### EventBroadcast(string, byte[])
Use to send event (netly event) to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L50)
```csharp title="Declaration"
void EventBroadcast(string name, byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.Byte[]` | *data* | Event data - bytes |

### EventBroadcast(string, string)
Use to send event (netly event) to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L57)
```csharp title="Declaration"
void EventBroadcast(string name, string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |

### EventBroadcast(string, string, Encoding)
Use to send event (netly event) to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L65)
```csharp title="Declaration"
void EventBroadcast(string name, string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| `System.Text.Encoding` | *encoding* | Event data encoding method |

### Data(Host, byte[])
Use to send raw data to a custom host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L72)
```csharp title="Declaration"
void Data(Host targetHost, byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *targetHost* | Target host |
| `System.Byte[]` | *data* | data - bytes |

### Data(Host, string)
Use to send raw data to a custom host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L79)
```csharp title="Declaration"
void Data(Host targetHost, string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *targetHost* | Target host |
| `System.String` | *data* | Data - string |

### Data(Host, string, Encoding)
Use to send raw data to a custom host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L87)
```csharp title="Declaration"
void Data(Host targetHost, string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *targetHost* | Target host |
| `System.String` | *data* | Data - string |
| `System.Text.Encoding` | *encoding* | Data encoding method |

### Event(Host, string, byte[])
Use to send event (netly event) to a custom host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L95)
```csharp title="Declaration"
void Event(Host host, string name, byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *host* | Target host |
| `System.String` | *name* | Event name |
| `System.Byte[]` | *data* | Event data - bytes |

### Event(Host, string, string)
Use to send event (netly event) to a custom host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L103)
```csharp title="Declaration"
void Event(Host targetHost, string name, string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *targetHost* | Target host |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |

### Event(Host, string, string, Encoding)
Use to send event (netly event) to a custom host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerTo.cs#L112)
```csharp title="Declaration"
void Event(Host targetHost, string name, string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Host](../Netly/Host) | *targetHost* | Target host |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| `System.Text.Encoding` | *encoding* | Event data encoding method |

