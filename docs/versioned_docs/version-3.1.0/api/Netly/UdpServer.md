---
title: Class UdpServer
sidebar_label: UdpServer
description: "Netly, Udp server implementation"
---
# Class UdpServer
Netly, Udp server implementation

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L14)
```csharp title="Declaration"
public class UdpServer
```
## Properties
### Host
Host container
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L19)
```csharp title="Declaration"
public Host Host { get; }
```
### Timeout
Connection timeout
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L24)
```csharp title="Declaration"
public int Timeout { get; }
```
### IsOpened
Return true when udp server is bind
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L29)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### UseConnection
Return true when udp connection is enabled
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L34)
```csharp title="Declaration"
public bool UseConnection { get; }
```
### Clients
Return array of connected client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L39)
```csharp title="Declaration"
public UdpClient[] Clients { get; }
```
## Methods
### Open(Host)
Start udp server (bind client)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L208)
```csharp title="Declaration"
public void Open(Host host)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* | Endpoint container |

### Close()
Close udp connection and disconnect all connected client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L247)
```csharp title="Declaration"
public void Close()
```
### ToData(byte[])
Broadcast raw data for all connected client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L293)
```csharp title="Declaration"
public void ToData(byte[] buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *buffer* | buffer (raw data) |

### ToData(string)
Broadcast raw data for all connected client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L308)
```csharp title="Declaration"
public void ToData(string buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *buffer* | buffer (raw data) |

### ToEvent(string, byte[])
Broadcast event (netly-event) for all connected client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L321)
```csharp title="Declaration"
public void ToEvent(string name, byte[] buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | event name |
| `System.Byte[]` | *buffer* | event buffer |

### ToEvent(string, string)
Broadcast event (netly-event) for all connected client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L334)
```csharp title="Declaration"
public void ToEvent(string name, string buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | event name |
| `System.String` | *buffer* | event buffer |

### OnOpen(Action)
Callback will called when is connection opened (server start bind)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L350)
```csharp title="Declaration"
public void OnOpen(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | callback |

### OnError(Action&lt;Exception&gt;)
Callback will called when connection isn't opened (error on open connection)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L365)
```csharp title="Declaration"
public void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Exception>` | *callback* | callback |

### OnClose(Action)
Callback will called when server closed connection
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L380)
```csharp title="Declaration"
public void OnClose(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | callback |

### OnData(Action&lt;UdpClient, byte[]&gt;)
Callback will called when a client receive raw data
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L395)
```csharp title="Declaration"
public void OnData(Action<UdpClient, byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.UdpClient,System.Byte[]>` | *callback* | callback |

### OnEvent(Action&lt;UdpClient, string, byte[]&gt;)
Callback will called when a client receive event (netly-event)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L410)
```csharp title="Declaration"
public void OnEvent(Action<UdpClient, string, byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.UdpClient,System.String,System.Byte[]>` | *callback* | callback |

### OnModify(Action&lt;Socket&gt;)
Callback will called after create Socket Instance, and before open connection


You can use the socket for modify default values
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L426)
```csharp title="Declaration"
public void OnModify(Action<Socket> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.Sockets.Socket>` | *callback* |

### OnEnter(Action&lt;UdpClient&gt;)
/ Callback will called when a client connect
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L441)
```csharp title="Declaration"
public void OnEnter(Action<UdpClient> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.UdpClient>` | *callback* | callback |

### OnExit(Action&lt;UdpClient&gt;)
Callback will called when a client disconnected from server
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpServer.cs#L456)
```csharp title="Declaration"
public void OnExit(Action<UdpClient> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.UdpClient>` | *callback* | callback |

