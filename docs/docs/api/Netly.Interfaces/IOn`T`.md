---
title: Interface IOn<T>
sidebar_label: IOn<T>
---
# Interface IOn&lt;T&gt;


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/INetly.On.cs#L5)
```csharp title="Declaration"
public interface IOn<out T>
```
## Methods
### Open(Action)
Use to handle connection open event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/INetly.On.cs#L11)
```csharp title="Declaration"
void Open(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | Callback |

### Error(Action&lt;Exception&gt;)
Use to handle connection error event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/INetly.On.cs#L17)
```csharp title="Declaration"
void Error(Action<Exception> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Exception>` | *callback* | Callback |

### Close(Action)
Use to handle connection close event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/INetly.On.cs#L23)
```csharp title="Declaration"
void Close(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | Callback |

### Modify(Action&lt;T&gt;)
Use to handle socket modification event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/INetly.On.cs#L29)
```csharp title="Declaration"
void Modify(Action<out T> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<<T>>` | *callback* | Callback |

