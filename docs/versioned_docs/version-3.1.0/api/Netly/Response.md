---
title: Class Response
sidebar_label: Response
---
# Class Response


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Response.cs#L9)
```csharp title="Declaration"
public class Response
```
## Fields
### RawResponse

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Response.cs#L11)
```csharp title="Declaration"
public readonly HttpListenerResponse RawResponse
```
## Methods
### Send(int, byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Response.cs#L18)
```csharp title="Declaration"
public void Send(int statusCode, byte[] buffer)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Int32` | *statusCode* |
| `System.Byte[]` | *buffer* |

### Send(int, string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Response.cs#L38)
```csharp title="Declaration"
public void Send(int statusCode, string buffer)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Int32` | *statusCode* |
| `System.String` | *buffer* |

