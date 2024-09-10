---
title: Class UdpClient
sidebar_label: UdpClient
description: "Netly, Udp client implementation"
---
# Class UdpClient
Netly, Udp client implementation

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L13)
```csharp title="Declaration"
public class UdpClient
```
## Properties
### IsOpened
Return true when connection is opened
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L23)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### Host
Host container
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L28)
```csharp title="Declaration"
public Host Host { get; }
```
### UseConnection
Return true when connection is enabled (using ping)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L33)
```csharp title="Declaration"
public bool UseConnection { get; }
```
### Timeout
Return timeout value, is milliseconds
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L38)
```csharp title="Declaration"
public int Timeout { get; }
```
## Fields
### UUID
Unique User Identifier, only server-side client
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L43)
```csharp title="Declaration"
public readonly string UUID
```
## Methods
### Open(Host)
Open udp connection
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L217)
```csharp title="Declaration"
public void Open(Host host)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* | Endpoint container |

### Close()
Close udp connection
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L255)
```csharp title="Declaration"
public void Close()
```
### ToData(byte[])
Send raw buffer
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L299)
```csharp title="Declaration"
public void ToData(byte[] buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *buffer* | buffer |

### ToData(string)
Send raw buffer
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L318)
```csharp title="Declaration"
public void ToData(string buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *buffer* | buffer |

### ToEvent(string, byte[])
Send event (netly-event)




You can create event using:

```csharp
Netly.Core.EventManager.Create(string name, byte[] data);
```

And you can verify is a buffer is event (netly-event) using: 

```csharp
// When name or data is null, it mean isn't event (netly-event) 
                                                                                                                                               (string name, byte[] data) = Netly.Core.EventManager.Verify(byte[] data);
```
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L340)
```csharp title="Declaration"
public void ToEvent(string name, byte[] buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | name of event |
| `System.Byte[]` | *buffer* | buffer of event |

### ToEvent(string, string)
Send event (netly-event)




You can create event using:

```csharp
Netly.Core.EventManager.Create(string name, byte[] data);
```

And you can verify is a buffer is event (netly-event) using: 

```csharp
// When name or data is null, it mean isn't event (netly-event) 
                                                                                                                                               (string name, byte[] data) = Netly.Core.EventManager.Verify(byte[] data);
```
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L362)
```csharp title="Declaration"
public void ToEvent(string name, string buffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | name of event |
| `System.String` | *buffer* | buffer of event |

### OnOpen(Action)
Callback will called when is connection opened
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L378)
```csharp title="Declaration"
public void OnOpen(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | callback |

### OnError(Action&lt;Exception&gt;)
Callback will called when connection isn't opened
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L393)
```csharp title="Declaration"
public void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Exception>` | *callback* | callback |

### OnClose(Action)
Callback will called when connection is closed
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L408)
```csharp title="Declaration"
public void OnClose(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | callback |

### OnData(Action&lt;byte[]&gt;)
Callback will called when receive raw data
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L423)
```csharp title="Declaration"
public void OnData(Action<byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Byte[]>` | *callback* | callback |

### OnEvent(Action&lt;string, byte[]&gt;)
Callback will called when receive event (netly-event)




You can create event using:

```csharp
Netly.Core.EventManager.Create(string name, byte[] data);
```

And you can verify is a buffer is event (netly-event) using: 

```csharp
// When name or data is null, it mean isn't event (netly-event) 
                                                                                                                                               (string name, byte[] data) = Netly.Core.EventManager.Verify(byte[] data);
```
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L447)
```csharp title="Declaration"
public void OnEvent(Action<string, byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.String,System.Byte[]>` | *callback* | callback |

### OnModify(Action&lt;Socket&gt;)
Callback will called after create Socket Instance, and before open connection


You can use the socket for modify default values
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Udp/UdpClient.cs#L463)
```csharp title="Declaration"
public void OnModify(Action<Socket> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.Sockets.Socket>` | *callback* |

