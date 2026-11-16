---
title: Class Server<T>
sidebar_label: Server<T>
---
# Class Server&lt;T&gt;


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L9)
```csharp title="Declaration"
public abstract class Server<T>
```
**Derived:**  
[Netly.TcpServer](../Netly/TcpServer)

## Properties
### Framing

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L12)
```csharp title="Declaration"
public bool Framing { get; protected set; }
```
### Host

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L13)
```csharp title="Declaration"
public Host Host { get; protected set; }
```
### Clients

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L14)
```csharp title="Declaration"
public List<T> Clients { get; protected set; }
```
### IsOpened

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L15)
```csharp title="Declaration"
public bool IsOpened { get; }
```
## Fields
### m_socket

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L17)
```csharp title="Declaration"
protected Socket m_socket
```
### m_connecting

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L18)
```csharp title="Declaration"
protected bool m_connecting
```
### m_closing

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L19)
```csharp title="Declaration"
protected bool m_closing
```
### m_closed

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L20)
```csharp title="Declaration"
protected bool m_closed
```
### m_opened

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L21)
```csharp title="Declaration"
protected bool m_opened
```
### m_lock

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L32)
```csharp title="Declaration"
protected readonly object m_lock
```
## Methods
### IsConnected()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L36)
```csharp title="Declaration"
protected virtual bool IsConnected()
```

##### Returns

`System.Boolean`
### Open(Host)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L41)
```csharp title="Declaration"
public virtual void Open(Host host)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* |

### Open(Host, int)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L46)
```csharp title="Declaration"
public virtual void Open(Host host, int backlogOrTimeout)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* |
| `System.Int32` | *backlogOrTimeout* |

### AcceptOrReceive()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L51)
```csharp title="Declaration"
protected virtual void AcceptOrReceive()
```
### Destroy()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L56)
```csharp title="Declaration"
protected virtual void Destroy()
```
### AddOrRemoveClient(T, bool)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L74)
```csharp title="Declaration"
protected virtual T AddOrRemoveClient(T client, bool removeClient)
```

##### Returns

`<T>`

##### Parameters

| Type | Name |
|:--- |:--- |
| `<T>` | *client* |
| `System.Boolean` | *removeClient* |

### Close()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L100)
```csharp title="Declaration"
public virtual void Close()
```
### ToData(byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L124)
```csharp title="Declaration"
public virtual void ToData(byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *data* |

### ToData(string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L135)
```csharp title="Declaration"
public virtual void ToData(string data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *data* |

### ToEvent(string, byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L140)
```csharp title="Declaration"
public virtual void ToEvent(string name, byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.Byte[]` | *data* |

### ToEvent(string, string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L151)
```csharp title="Declaration"
public virtual void ToEvent(string name, string data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *data* |

### OnError(Action&lt;Exception&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L158)
```csharp title="Declaration"
public virtual void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Exception>` | *callback* |

### OnOpen(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L166)
```csharp title="Declaration"
public virtual void OnOpen(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnClose(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L174)
```csharp title="Declaration"
public virtual void OnClose(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnEnter(Action&lt;T&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L182)
```csharp title="Declaration"
public virtual void OnEnter(Action<T> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<<T>>` | *callback* |

### OnExit(Action&lt;T&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L190)
```csharp title="Declaration"
public virtual void OnExit(Action<T> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<<T>>` | *callback* |

### OnData(Action&lt;T, byte[]&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L198)
```csharp title="Declaration"
public virtual void OnData(Action<T, byte[]> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<<T>,System.Byte[]>` | *callback* |

### OnEvent(Action&lt;T, string, byte[]&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L206)
```csharp title="Declaration"
public virtual void OnEvent(Action<T, string, byte[]> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<<T>,System.String,System.Byte[]>` | *callback* |

### OnModify(Action&lt;Socket&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Abstract/Server.cs#L214)
```csharp title="Declaration"
public virtual void OnModify(Action<Socket> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.Sockets.Socket>` | *callback* |

