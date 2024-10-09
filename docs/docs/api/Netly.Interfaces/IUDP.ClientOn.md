---
title: Interface IUDP.ClientOn
sidebar_label: IUDP.ClientOn
description: "UDP Client callbacks container (interface)"
---
# Interface IUDP.ClientOn
UDP Client callbacks container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientOn.cs#L11)
```csharp title="Declaration"
public interface IUDP.ClientOn : IOn<Socket>
```
## Methods
### Data(Action&lt;byte[]&gt;)
Use to handle raw data receiving event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientOn.cs#L17)
```csharp title="Declaration"
void Data(Action<byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Byte[]>` | *callback* | Callback function |

### Event(Action&lt;string, byte[]&gt;)
Use to handle event receive event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ClientOn.cs#L23)
```csharp title="Declaration"
void Event(Action<string, byte[]> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.String,System.Byte[]>` | *callback* | Callback function |

