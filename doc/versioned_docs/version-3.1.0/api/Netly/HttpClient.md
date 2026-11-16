---
title: Class HttpClient
sidebar_label: HttpClient
---
# Class HttpClient


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L11)
```csharp title="Declaration"
public class HttpClient
```
## Properties
### Timeout

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L15)
```csharp title="Declaration"
public int Timeout { get; set; }
```
### Headers

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L29)
```csharp title="Declaration"
public KeyValueContainer Headers { get; }
```
### Queries

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L30)
```csharp title="Declaration"
public KeyValueContainer Queries { get; }
```
### Body

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L31)
```csharp title="Declaration"
public RequestBody Body { get; set; }
```
### Method

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L32)
```csharp title="Declaration"
public HttpMethod Method { get; }
```
## Methods
### OnSuccess(Action&lt;Request&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L47)
```csharp title="Declaration"
public void OnSuccess(Action<Request> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<Netly.Request>` | *callback* |

### OnError(Action&lt;Exception&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L54)
```csharp title="Declaration"
public void OnError(Action<Exception> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Exception>` | *callback* |

### OnModify(Action&lt;HttpClient&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L59)
```csharp title="Declaration"
public void OnModify(Action<HttpClient> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Net.Http.HttpClient>` | *callback* |

### Send(string, Uri)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/HttpClient.cs#L64)
```csharp title="Declaration"
public void Send(string method, Uri uri)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *method* |
| `System.Uri` | *uri* |

