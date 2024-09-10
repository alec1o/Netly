---
title: Class Request
sidebar_label: Request
---
# Class Request


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L10)
```csharp title="Declaration"
public class Request
```
## Fields
### RawRequest

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L12)
```csharp title="Declaration"
public readonly HttpListenerRequest RawRequest
```
### RawResponse

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L13)
```csharp title="Declaration"
public readonly HttpResponseMessage RawResponse
```
### Headers

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L15)
```csharp title="Declaration"
public readonly KeyValueContainer Headers
```
### Queries

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L16)
```csharp title="Declaration"
public readonly KeyValueContainer Queries
```
### Cookies

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L17)
```csharp title="Declaration"
public readonly Cookie[] Cookies
```
### Method

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L18)
```csharp title="Declaration"
public readonly HttpMethod Method
```
### Url

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L19)
```csharp title="Declaration"
public readonly string Url
```
### Path

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L20)
```csharp title="Declaration"
public readonly string Path
```
### LocalEndPoint

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L21)
```csharp title="Declaration"
public readonly Host LocalEndPoint
```
### RemoteEndPoint

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L22)
```csharp title="Declaration"
public readonly Host RemoteEndPoint
```
### IsWebSocket

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L23)
```csharp title="Declaration"
public readonly bool IsWebSocket
```
### IsLocalRequest

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L24)
```csharp title="Declaration"
public readonly bool IsLocalRequest
```
### IsEncrypted

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L25)
```csharp title="Declaration"
public readonly bool IsEncrypted
```
### Body

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L26)
```csharp title="Declaration"
public readonly RequestBody Body
```
### StatusCode

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L27)
```csharp title="Declaration"
public readonly int StatusCode
```
## Methods
### ComparePath(string)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/HTTP/Request.cs#L125)
```csharp title="Declaration"
public bool ComparePath(string path)
```

##### Returns

`System.Boolean`

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.String` | *path* |

