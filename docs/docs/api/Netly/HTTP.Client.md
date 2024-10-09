---
title: Class HTTP.Client
sidebar_label: HTTP.Client
description: "HTTP Client"
---
# Class HTTP.Client
HTTP Client

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L8)
```csharp title="Declaration"
public class HTTP.Client
```
## Properties
### Headers
Fetch Header
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L34)
```csharp title="Declaration"
public Dictionary<string, string> Headers { get; }
```
### Queries
Fetch Queries
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L39)
```csharp title="Declaration"
public Dictionary<string, string> Queries { get; }
```
### Timeout
Fetch Timeout (Milliseconds)


Default is: 15000 (15 Seconds)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L45)
```csharp title="Declaration"
public int Timeout { get; set; }
```
### IsOpened
Is true while fetch operation it's working
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L54)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### On
Fetch callback handler
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L59)
```csharp title="Declaration"
public IHTTP.ClientOn On { get; }
```
### To
Fetch action creator
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Client/HTTP.Client.cs#L64)
```csharp title="Declaration"
public IHTTP.ClientTo To { get; }
```
