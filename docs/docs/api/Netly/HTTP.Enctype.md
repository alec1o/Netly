---
title: Enum HTTP.Enctype
sidebar_label: HTTP.Enctype
description: "HTTP Enctype Header"
---
# Enum HTTP.Enctype
HTTP Enctype Header

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L8)
```csharp title="Declaration"
public enum HTTP.Enctype : sbyte
```
## Fields
### Unknown
Used when the enctype is not recognized or unsupported.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L13)
```csharp title="Declaration"
Unknown = -1
```
### None
No encoding is specified.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L18)
```csharp title="Declaration"
None = 0
```
### UrlEncoded
Default for form submissions. Sends data as `application/x-www-form-urlencoded`.
(Parser Supported)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L24)
```csharp title="Declaration"
UrlEncoded = 1
```
### Multipart
Sends data as `multipart/form-data`. Used for file uploads.
(Parser Supported)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L30)
```csharp title="Declaration"
Multipart = 2
```
### PlainText
Sends data as plain text using `text/plain`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L35)
```csharp title="Declaration"
PlainText = 3
```
### Json
Sends data as JSON using `application/json`. Common in RESTful APIs.
(Parser Supported)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L41)
```csharp title="Declaration"
Json = 4
```
### Xml
Sends data as XML using `application/xml`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L46)
```csharp title="Declaration"
Xml = 5
```
### OctetStream
Sends binary data using `application/octet-stream`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L51)
```csharp title="Declaration"
OctetStream = 6
```
### Csv
Sends CSV (Comma Separated Values) using `text/csv`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L56)
```csharp title="Declaration"
Csv = 7
```
### Html
Sends HTML content using `text/html`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L61)
```csharp title="Declaration"
Html = 8
```
### Pdf
Sends data as `application/pdf`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L66)
```csharp title="Declaration"
Pdf = 9
```
### Javascript
Sends JavaScript code using `application/javascript`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L71)
```csharp title="Declaration"
Javascript = 10
```
### Yaml
Sends YAML data using `application/x-yaml`.
(Parser Supported)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L77)
```csharp title="Declaration"
Yaml = 11
```
### GraphQL
Sends GraphQL queries using `application/graphql`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L82)
```csharp title="Declaration"
GraphQL = 12
```
### SoapXml
Sends SOAP XML data using `application/soap+xml`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L87)
```csharp title="Declaration"
SoapXml = 13
```
### Zip
Sends ZIP files using `application/zip`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L92)
```csharp title="Declaration"
Zip = 14
```
### MultipartRelated
Sends form data encoded as `multipart/related`. Typically used in APIs or email-related transfers, where multiple
parts of a message are related but differ in content type. Requires combining various parts.
(Parser Supported)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L99)
```csharp title="Declaration"
MultipartRelated = 15
```
### WebAssembly
Sends WebAssembly files using `application/wasm`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L104)
```csharp title="Declaration"
WebAssembly = 16
```
### Markdown
Sends Markdown text using `text/markdown`.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/HTTP.Enctype.cs#L109)
```csharp title="Declaration"
Markdown = 17
```
