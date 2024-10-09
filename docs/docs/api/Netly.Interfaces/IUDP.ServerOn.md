---
title: Interface IUDP.ServerOn
sidebar_label: IUDP.ServerOn
description: "UDP Server callbacks container (interface)"
---
# Interface IUDP.ServerOn
UDP Server callbacks container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerOn.cs#L11)
```csharp title="Declaration"
public interface IUDP.ServerOn : IOn<Socket>
```
## Methods
### Accept(Action&lt;Client&gt;)
Use to handle new client onboarding
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.ServerOn.cs#L17)
```csharp title="Declaration"
void Accept(Action<IUDP.Client> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.Interfaces.IUDP.Client>` | *callback* | Callback function |

