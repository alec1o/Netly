---
title: Interface IHTTP.ClientResponse
sidebar_label: IHTTP.ClientResponse
---
# Interface IHTTP.ClientResponse


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L9)
```csharp title="Declaration"
public interface IHTTP.ClientResponse
```
## Properties
### NativeResponse
Native Response Object
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L14)
```csharp title="Declaration"
HttpResponseMessage NativeResponse { get; }
```
### NativeClient
Native Client Object
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L19)
```csharp title="Declaration"
HttpClient NativeClient { get; }
```
### Encoding
Response encoding
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L24)
```csharp title="Declaration"
Encoding Encoding { get; }
```
### Headers
Response Headers
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L29)
```csharp title="Declaration"
Dictionary<string, string> Headers { get; }
```
### Queries
Response Queries
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L34)
```csharp title="Declaration"
Dictionary<string, string> Queries { get; }
```
### Method
Request Http Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L39)
```csharp title="Declaration"
HttpMethod Method { get; }
```
### Enctype
Response Enctype
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L44)
```csharp title="Declaration"
HTTP.Enctype Enctype { get; }
```
### Url
Request Url
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L49)
```csharp title="Declaration"
string Url { get; }
```
### Path
Request Path
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L54)
```csharp title="Declaration"
string Path { get; }
```
### IsLocalRequest
Return true if request is same host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L59)
```csharp title="Declaration"
bool IsLocalRequest { get; }
```
### IsEncrypted
Return true if connection is encrypted e.g SSL or TLS protocol
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L64)
```csharp title="Declaration"
bool IsEncrypted { get; }
```
### Body
Request Body
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L69)
```csharp title="Declaration"
IHTTP.Body Body { get; }
```
### Status
Http Status Code. 


if value is -1 mean: (not applicable on context).
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientResponse.cs#L75)
```csharp title="Declaration"
int Status { get; }
```
