---
title: Class HttpServer
sidebar_label: HttpServer
---
# Class HttpServer


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L11)
```csharp title="Declaration"
public class HttpServer
```
## Properties
### IsOpen

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L22)
```csharp title="Declaration"
public bool IsOpen { get; }
```
### Host

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L23)
```csharp title="Declaration"
public Uri Host { get; }
```
## Methods
### Open(Uri)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L32)
```csharp title="Declaration"
public void Open(Uri host)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Uri` | *host* |

### OnOpen(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L68)
```csharp title="Declaration"
public void OnOpen(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### OnError(Action&lt;Exception&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L75)
```csharp title="Declaration"
public void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Exception>` | *callback* |

### Close()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L82)
```csharp title="Declaration"
public void Close()
```
### OnClose(Action)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L115)
```csharp title="Declaration"
public void OnClose(Action callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *callback* |

### MapAll(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L122)
```csharp title="Declaration"
public void MapAll(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapGet(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L127)
```csharp title="Declaration"
public void MapGet(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapPut(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L132)
```csharp title="Declaration"
public void MapPut(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapHead(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L137)
```csharp title="Declaration"
public void MapHead(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapPost(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L142)
```csharp title="Declaration"
public void MapPost(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapPatch(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L147)
```csharp title="Declaration"
public void MapPatch(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapTrace(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L157)
```csharp title="Declaration"
public void MapTrace(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapDelete(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L162)
```csharp title="Declaration"
public void MapDelete(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapOptions(string, Action&lt;Request, Response&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L167)
```csharp title="Declaration"
public void MapOptions(string path, Action<Request, Response> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.Response>` | *callback* |

### MapWebSocket(string, Action&lt;Request, WebSocketClient&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpServer.cs#L178)
```csharp title="Declaration"
public void MapWebSocket(string path, Action<Request, WebSocketClient> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |
| `System.Action<Netly.Request,Netly.WebSocketClient>` | *callback* |

