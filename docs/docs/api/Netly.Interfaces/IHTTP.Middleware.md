---
title: Interface IHTTP.Middleware
sidebar_label: IHTTP.Middleware
---
# Interface IHTTP.Middleware


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Middleware.cs#L7)
```csharp title="Declaration"
public interface IHTTP.Middleware
```
## Properties
### Middlewares
Middleware array
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Middleware.cs#L12)
```csharp title="Declaration"
IHTTP.MiddlewareDescriptor[] Middlewares { get; }
```
## Methods
### Add(Action&lt;ServerRequest, ServerResponse, Action&gt;)
Add global middleware handler
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Middleware.cs#L19)
```csharp title="Declaration"
bool Add(Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> middleware)
```

##### Returns

`System.Boolean`: true if callback added successful
##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse,System.Action>` | *middleware* | Middleware handler |

### Add(string, Action&lt;ServerRequest, ServerResponse, Action&gt;)
Add local middleware handler
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Middleware.cs#L28)
```csharp title="Declaration"
bool Add(string path, Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> middleware)
```

##### Returns

`System.Boolean`: true if callback added successful
##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *path* | Route path |
| `System.Action<Netly.Interfaces.IHTTP.ServerRequest,Netly.Interfaces.IHTTP.ServerResponse,System.Action>` | *middleware* | Middleware handler |

