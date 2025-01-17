---
title: Interface ITCP.ServerOn
sidebar_label: ITCP.ServerOn
---
# Interface ITCP.ServerOn


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerOn.cs#L8)
```csharp title="Declaration"
public interface ITCP.ServerOn : IOn<Socket>
```
## Methods
### Accept(Action&lt;Client&gt;)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ServerOn.cs#L10)
```csharp title="Declaration"
void Accept(Action<ITCP.Client> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<Netly.Interfaces.ITCP.Client>` | *callback* |

