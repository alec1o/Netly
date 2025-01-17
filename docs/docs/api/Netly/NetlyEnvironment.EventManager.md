---
title: Class NetlyEnvironment.EventManager
sidebar_label: NetlyEnvironment.EventManager
---
# Class NetlyEnvironment.EventManager


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/partials/NetlyEnvironment.EventManager.cs#L8)
```csharp title="Declaration"
public static class NetlyEnvironment.EventManager
```
## Methods
### Verify(byte[])

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/partials/NetlyEnvironment.EventManager.cs#L12)
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

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/partials/NetlyEnvironment.EventManager.cs#L26)
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

