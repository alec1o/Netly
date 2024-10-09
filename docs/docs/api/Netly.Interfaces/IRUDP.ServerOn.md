---
title: Interface IRUDP.ServerOn
sidebar_label: IRUDP.ServerOn
description: "RUDP Server callbacks container (interface)"
---
# Interface IRUDP.ServerOn
RUDP Server callbacks container (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerOn.cs#L11)
```csharp title="Declaration"
public interface IRUDP.ServerOn : IOn<Socket>
```
## Methods
### Accept(Action&lt;Client&gt;)
Use to handle new client onboarding
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.ServerOn.cs#L17)
```csharp title="Declaration"
void Accept(Action<IRUDP.Client> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.Interfaces.IRUDP.Client>` | *callback* | Callback function |

