---
title: Interface IHTTP.Body
sidebar_label: IHTTP.Body
---
# Interface IHTTP.Body


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L7)
```csharp title="Declaration"
public interface IHTTP.Body
```
## Properties
### Encoding
Body Encoding


&lt;i&gt;If not found in HTTP header UTF-8 is used by Default&lt;/i&gt;
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L13)
```csharp title="Declaration"
Encoding Encoding { get; }
```
### Enctype
Enctype type
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L18)
```csharp title="Declaration"
HTTP.Enctype Enctype { get; }
```
### Text
Text buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L23)
```csharp title="Declaration"
string Text { get; }
```
### Binary
Binary buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L28)
```csharp title="Declaration"
byte[] Binary { get; }
```
### Parser
Enctype Parser: Make easy and seamless parse JSON, YML, UrlEncoded, and more!
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L33)
```csharp title="Declaration"
IHTTP.EnctypeParser Parser { get; }
```
