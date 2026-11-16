---
title: Class EventManager
sidebar_label: EventManager
---
# Class EventManager


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/EventManager.cs#L7)
```csharp title="Declaration"
public static class EventManager
```
## Methods
### Verify(byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/EventManager.cs#L11)
```csharp title="Declaration"
public static (string name, byte[] data) Verify(byte[] buffer)
```

##### Returns

`System.ValueTuple<System.String,System.Byte[]>`

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *buffer* |

### Create(string, byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/EventManager.cs#L25)
```csharp title="Declaration"
public static byte[] Create(string name, byte[] data)
```

##### Returns

`System.Byte[]`

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *name* |
| `System.Byte[]` | *data* |

