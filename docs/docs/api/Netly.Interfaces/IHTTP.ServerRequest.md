---
title: Interface IHTTP.ServerRequest
sidebar_label: IHTTP.ServerRequest
---
# Interface IHTTP.ServerRequest


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L10)
```csharp title="Declaration"
public interface IHTTP.ServerRequest
```
## Properties
### Encoding
Request encoding
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L15)
```csharp title="Declaration"
Encoding Encoding { get; }
```
### Headers
Request Headers
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L20)
```csharp title="Declaration"
Dictionary<string, string> Headers { get; }
```
### Queries
Request Queries
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L25)
```csharp title="Declaration"
Dictionary<string, string> Queries { get; }
```
### Params
Request Params
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L30)
```csharp title="Declaration"
Dictionary<string, string> Params { get; }
```
### Cookies
Request Cookies
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L35)
```csharp title="Declaration"
Cookie[] Cookies { get; }
```
### Method
Request Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L40)
```csharp title="Declaration"
HttpMethod Method { get; }
```
### Url
Request Url
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L45)
```csharp title="Declaration"
string Url { get; }
```
### Path
Request Path
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L50)
```csharp title="Declaration"
string Path { get; }
```
### LocalEndPoint
Request (local end point)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L55)
```csharp title="Declaration"
Host LocalEndPoint { get; }
```
### RemoteEndPoint
Request (remote end point)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L60)
```csharp title="Declaration"
Host RemoteEndPoint { get; }
```
### IsWebSocket
Return true if request is websocket
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L65)
```csharp title="Declaration"
bool IsWebSocket { get; }
```
### IsLocalRequest
Return true if request is same host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L70)
```csharp title="Declaration"
bool IsLocalRequest { get; }
```
### IsEncrypted
Return true if connection is encrypted e.g SSL or TLS protocol
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L75)
```csharp title="Declaration"
bool IsEncrypted { get; }
```
### Body
Request Body
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L80)
```csharp title="Declaration"
IHTTP.Body Body { get; }
```
### Enctype
Request Enctype
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerRequest.cs#L85)
```csharp title="Declaration"
HTTP.Enctype Enctype { get; }
```
