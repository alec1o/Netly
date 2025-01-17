---
title: Interface IHTTP.ServerRequest
sidebar_label: IHTTP.ServerRequest
---
# Interface IHTTP.ServerRequest


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L11)
```csharp title="Declaration"
public interface IHTTP.ServerRequest
```
## Properties
### Encoding
Request encoding
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L16)
```csharp title="Declaration"
Encoding Encoding { get; }
```
### Headers
Request Headers
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L21)
```csharp title="Declaration"
Dictionary<string, string> Headers { get; }
```
### Queries
Request Queries
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L26)
```csharp title="Declaration"
Dictionary<string, string> Queries { get; }
```
### Params
Request Params
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L31)
```csharp title="Declaration"
Dictionary<string, string> Params { get; }
```
### Cookies
Request Cookies
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L36)
```csharp title="Declaration"
Cookie[] Cookies { get; }
```
### Method
Request Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L41)
```csharp title="Declaration"
HttpMethod Method { get; }
```
### Url
Request Url
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L46)
```csharp title="Declaration"
string Url { get; }
```
### Path
Request Path
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L51)
```csharp title="Declaration"
string Path { get; }
```
### LocalEndPoint
Request (local end point)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L56)
```csharp title="Declaration"
Host LocalEndPoint { get; }
```
### RemoteEndPoint
Request (remote end point)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L61)
```csharp title="Declaration"
Host RemoteEndPoint { get; }
```
### IsWebSocket
Return true if request is websocket
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L66)
```csharp title="Declaration"
bool IsWebSocket { get; }
```
### IsLocalRequest
Return true if request is same host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L71)
```csharp title="Declaration"
bool IsLocalRequest { get; }
```
### IsEncrypted
Return true if connection is encrypted e.g. SSL or TLS protocol
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L76)
```csharp title="Declaration"
bool IsEncrypted { get; }
```
### Body
Request Body
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L81)
```csharp title="Declaration"
IHTTP.Body Body { get; }
```
### Enctype
Request Enctype
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L87)
```csharp title="Declaration"
HTTP.Enctype Enctype { get; }
```
