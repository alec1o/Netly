---
title: Interface IHTTP.Map
sidebar_label: IHTTP.Map
---
# Interface IHTTP.Map


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L7)
```csharp title="Declaration"
public interface IHTTP.Map
```
## Methods
### WebSocket(string, Action&lt;ServerRequest, WebSocket&gt;)
Handle WebSocket from Path
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L14)
```csharp title="Declaration"
void WebSocket(string path, Action<IHTTP.ServerRequest, IHTTP.WebSocket> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.WebSocket>` | *callback* | Response Callback |

### All(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle All Http Method from Path
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L21)
```csharp title="Declaration"
void All(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Get(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Get) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L28)
```csharp title="Declaration"
void Get(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Put(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Put) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L35)
```csharp title="Declaration"
void Put(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Head(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Head) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L42)
```csharp title="Declaration"
void Head(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Post(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Post) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L49)
```csharp title="Declaration"
void Post(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Patch(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Patch) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L56)
```csharp title="Declaration"
void Patch(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Delete(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Delete) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L63)
```csharp title="Declaration"
void Delete(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Trace(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Trace) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L70)
```csharp title="Declaration"
void Trace(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

### Options(string, Action&lt;ServerRequest, ServerResponse&gt;)
Handle (Options) Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Map.cs#L77)
```csharp title="Declaration"
void Options(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse> callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Request Path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse>` | *callback* | Response Callback |

