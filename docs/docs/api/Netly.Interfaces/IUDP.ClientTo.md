---
title: Interface IUDP.ClientTo
sidebar_label: IUDP.ClientTo
description: "UDP Client actions container (interface)"
---
# Interface IUDP.ClientTo
UDP Client actions container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L11)
```csharp title="Declaration"
public interface IUDP.ClientTo
```
## Methods
### Open(Host)
Use to open connection (if disconnected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L18)
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
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L24)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### Data(byte[])
Send raw data
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L30)
```csharp title="Declaration"
void Data(byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *data* | data - bytes |

### Data(string)
Send raw data
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L36)
```csharp title="Declaration"
void Data(string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |

### Data(string, Encoding)
Send raw data
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L43)
```csharp title="Declaration"
void Data(string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |
| `System.Text.Encoding` | *encoding* | Data encoding method |

### Event(string, byte[])
Send event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L50)
```csharp title="Declaration"
void Event(string name, byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.Byte[]` | *data* | Event data - bytes |

### Event(string, string)
Send event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L57)
```csharp title="Declaration"
void Event(string name, string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |

### Event(string, string, Encoding)
Send event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientTo.cs#L65)
```csharp title="Declaration"
void Event(string name, string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| `System.Text.Encoding` | *encoding* | Event data encoding method |

