---
title: Class RequestBody
sidebar_label: RequestBody
---
# Class RequestBody


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/RequestBody.cs#L12)
```csharp title="Declaration"
public class RequestBody
```
## Properties
### Buffer

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/RequestBody.cs#L14)
```csharp title="Declaration"
public byte[] Buffer { get; }
```
### Length

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/RequestBody.cs#L15)
```csharp title="Declaration"
public int Length { get; }
```
### Form

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/RequestBody.cs#L16)
```csharp title="Declaration"
public KeyValueContainer Form { get; }
```
### PlainText

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/RequestBody.cs#L17)
```csharp title="Declaration"
public string PlainText { get; }
```
## Methods
### GetHttpContent()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/RequestBody.cs#L58)
```csharp title="Declaration"
public HttpContent GetHttpContent()
```

##### Returns

`System.Net.Http.HttpContent`
