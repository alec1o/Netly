---
title: Interface ITCP.ClientTo
sidebar_label: ITCP.ClientTo
---
# Interface ITCP.ClientTo


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L8)
```csharp title="Declaration"
public interface ITCP.ClientTo
```
## Methods
### Open(Host)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L10)
```csharp title="Declaration"
Task Open(Host host)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Host](../Netly/Host) | *host* |

### Close()

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L11)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
### Data(byte[])

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L12)
```csharp title="Declaration"
void Data(byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *data* |

### Encryption(bool)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L13)
```csharp title="Declaration"
void Encryption(bool enable)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Boolean` | *enable* |

### Data(string)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L14)
```csharp title="Declaration"
void Data(string data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *data* |

### Data(string, Encoding)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L15)
```csharp title="Declaration"
void Data(string data, Encoding encoding)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *data* |
| `System.Text.Encoding` | *encoding* |

### Event(string, byte[])

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L16)
```csharp title="Declaration"
void Event(string name, byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.Byte[]` | *data* |

### Event(string, string)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L17)
```csharp title="Declaration"
void Event(string name, string data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *data* |

### Event(string, string, Encoding)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientTo.cs#L18)
```csharp title="Declaration"
void Event(string name, string data, Encoding encoding)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.String` | *data* |
| `System.Text.Encoding` | *encoding* |

