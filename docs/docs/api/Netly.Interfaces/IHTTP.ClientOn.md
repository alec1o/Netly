---
title: Interface IHTTP.ClientOn
sidebar_label: IHTTP.ClientOn
description: "HTTP.Client callbacks container"
---
# Interface IHTTP.ClientOn
HTTP.Client callbacks container

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientOn.cs#L11)
```csharp title="Declaration"
public interface IHTTP.ClientOn
```
## Methods
### Open(Action&lt;ClientResponse&gt;)
Handle fetch response
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientOn.cs#L17)
```csharp title="Declaration"
void Open(Action<IHTTP.ClientResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.Interfaces.IHTTP.ClientResponse>` | *callback* | Callback |

### Error(Action&lt;Exception&gt;)
Use to handle connection error event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientOn.cs#L23)
```csharp title="Declaration"
void Error(Action<Exception> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Exception>` | *callback* | Callback |

### Close(Action)
Use to handle connection close event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientOn.cs#L29)
```csharp title="Declaration"
void Close(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | Callback |

### Modify(Action&lt;HttpClient&gt;)
Use to handle socket modification event
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientOn.cs#L35)
```csharp title="Declaration"
void Modify(Action<HttpClient> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Net.Http.HttpClient>` | *callback* | Callback |

