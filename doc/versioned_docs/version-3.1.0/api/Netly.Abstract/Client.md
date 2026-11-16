---
title: Class Client
sidebar_label: Client
---
# Class Client


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L11)
```csharp title="Declaration"
public abstract class Client
```
**Derived:**  
[Netly.TcpClient](../Netly/TcpClient)

## Properties
### Framing

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L15)
```csharp title="Declaration"
public bool Framing { get; protected set; }
```
### UUID

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L16)
```csharp title="Declaration"
public string UUID { get; protected set; }
```
### Host

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L17)
```csharp title="Declaration"
public Host Host { get; protected set; }
```
### IsOpened

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L19)
```csharp title="Declaration"
public bool IsOpened { get; }
```
## Fields
### m_socket

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L21)
```csharp title="Declaration"
protected Socket m_socket
```
### m_stream

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L22)
```csharp title="Declaration"
protected NetworkStream m_stream
```
### m_sslStream

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L23)
```csharp title="Declaration"
protected SslStream m_sslStream
```
### m_closed

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L24)
```csharp title="Declaration"
protected bool m_closed
```
### m_closing

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L25)
```csharp title="Declaration"
protected bool m_closing
```
### m_connecting

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L26)
```csharp title="Declaration"
protected bool m_connecting
```
### m_serverMode

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L27)
```csharp title="Declaration"
protected bool m_serverMode
```
## Methods
### IsConnected()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L39)
```csharp title="Declaration"
protected virtual bool IsConnected()
```

##### Returns

`System.Boolean`
### Open(Host)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L44)
```csharp title="Declaration"
public virtual void Open(Host host)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* |

### Receive()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L56)
```csharp title="Declaration"
protected virtual void Receive()
```
### Close()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L61)
```csharp title="Declaration"
public virtual void Close()
```
### ToData(byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L71)
```csharp title="Declaration"
public virtual void ToData(byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *data* |

### ToData(string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L80)
```csharp title="Declaration"
public virtual void ToData(string data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *data* |

### ToEvent(string, byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L85)
```csharp title="Declaration"
public virtual void ToEvent(string name, byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.Byte[]` | *data* |

### ToEvent(string, string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L90)
```csharp title="Declaration"
public virtual void ToEvent(string name, string data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *data* |

### Destroy()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L95)
```csharp title="Declaration"
protected virtual void Destroy()
```
### OnError(Action&lt;Exception&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L117)
```csharp title="Declaration"
public virtual void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Exception>` | *callback* |

### OnOpen(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L125)
```csharp title="Declaration"
public virtual void OnOpen(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnClose(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L133)
```csharp title="Declaration"
public virtual void OnClose(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnData(Action&lt;byte[]&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L141)
```csharp title="Declaration"
public virtual void OnData(Action<byte[]> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Byte[]>` | *callback* |

### OnEvent(Action&lt;string, byte[]&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L149)
```csharp title="Declaration"
public virtual void OnEvent(Action<string, byte[]> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.String,System.Byte[]>` | *callback* |

### OnModify(Action&lt;Socket&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Client.cs#L157)
```csharp title="Declaration"
public virtual void OnModify(Action<Socket> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.Sockets.Socket>` | *callback* |

