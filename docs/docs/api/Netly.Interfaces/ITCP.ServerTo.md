---
title: Interface ITCP.ServerTo
sidebar_label: ITCP.ServerTo
---
# Interface ITCP.ServerTo


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L9)
```csharp title="Declaration"
public interface ITCP.ServerTo
```
## Methods
### Open(Host)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L11)
```csharp title="Declaration"
Task Open(Host host)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Host](../Netly/Host) | *host* |

### Open(Host, int)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L12)
```csharp title="Declaration"
Task Open(Host host, int backlog)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Host](../Netly/Host) | *host* |
| `System.Int32` | *backlog* |

### Close()

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L13)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### Encryption(bool, byte[], string, SslProtocols)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L14)
```csharp title="Declaration"
void Encryption(bool enableEncryption, byte[] pfxCertificate, string pfxPassword, SslProtocols protocols)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Boolean` | *enableEncryption* |
| `System.Byte[]` | *pfxCertificate* |
| `System.String` | *pfxPassword* |
| `System.Security.Authentication.SslProtocols` | *protocols* |

### DataBroadcast(byte[])
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L20)
```csharp title="Declaration"
void DataBroadcast(byte[] data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *data* | data - bytes |

### DataBroadcast(string)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L26)
```csharp title="Declaration"
void DataBroadcast(string data)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *data* | Data - string |

### DataBroadcast(string, Encoding)
Use to send raw data to all connected clients
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L33)
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
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L40)
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
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L47)
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
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerTo.cs#L55)
```csharp title="Declaration"
void EventBroadcast(string name, string data, Encoding encoding)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | Event name |
| `System.String` | *data* | Event data - string |
| `System.Text.Encoding` | *encoding* | Event data encoding method |

