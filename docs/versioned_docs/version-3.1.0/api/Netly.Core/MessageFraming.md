---
title: Class MessageFraming
sidebar_label: MessageFraming
description: "Netly: Message framing"
---
# Class MessageFraming
Netly: Message framing

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L11)
```csharp title="Declaration"
public class MessageFraming
```
## Properties
### MaxSize
Max buffer size (prevent memory leak). Default is 8.388.608 (8MB)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L31)
```csharp title="Declaration"
public static int MaxSize { get; set; }
```
### UdpBuffer
Max udp package (prevent memory leak). Default is 1.048.576 (1MB)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L40)
```csharp title="Declaration"
public static int UdpBuffer { get; set; }
```
## Fields
### PREFIX
Netly message framing prefix: [ 0, 8, 16, 32, 64, 128 ] (6 byte overhead/size)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L16)
```csharp title="Declaration"
public static readonly byte[] PREFIX
```
## Methods
### CreateMessage(byte[])
Create message framing bytes (attach prefix)


Protocol:


 [ 0, 8, 16, 32, 64, 128 ] + [ BUFFER_LENGTH ] + [ BUFFER ]
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L53)
```csharp title="Declaration"
public static byte[] CreateMessage(byte[] value)
```

##### Returns

`System.Byte[]`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *value* | Input |

### OnData(Action&lt;byte[]&gt;)
Called when have data
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L63)
```csharp title="Declaration"
public void OnData(Action<byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Byte[]>` | *callback* | Callback |

### OnError(Action&lt;Exception&gt;)
Called when have error
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L72)
```csharp title="Declaration"
public void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Exception>` | *callback* | Callback |

### Clear()
Clear buffer
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L80)
```csharp title="Declaration"
public void Clear()
```
### Add(byte[])
Add buffer in flow
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MessageFraming.cs#L105)
```csharp title="Declaration"
public void Add(byte[] buffer)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *buffer* |

