---
title: Interface IRUDP.ClientOn
sidebar_label: IRUDP.ClientOn
description: "RUDP Client callbacks container (interface)"
---
# Interface IRUDP.ClientOn
RUDP Client callbacks container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientOn.cs#L11)
```csharp title="Declaration"
public interface IRUDP.ClientOn : IOn<Socket>
```
## Methods
### Data(Action&lt;byte[], MessageType&gt;)
Use to handle raw data receiving event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientOn.cs#L17)
```csharp title="Declaration"
void Data(Action<byte[], RUDP.MessageType> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Byte[],Netly.RUDP.MessageType>` | *callback* | Callback function |

### Event(Action&lt;string, byte[], MessageType&gt;)
Use to handle event receive event (netly event)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ClientOn.cs#L23)
```csharp title="Declaration"
void Event(Action<string, byte[], RUDP.MessageType> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.String,System.Byte[],Netly.RUDP.MessageType>` | *callback* | Callback function |

