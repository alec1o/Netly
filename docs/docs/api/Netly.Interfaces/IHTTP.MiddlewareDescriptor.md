---
title: Interface IHTTP.MiddlewareDescriptor
sidebar_label: IHTTP.MiddlewareDescriptor
description: "Middleware info container"
---
# Interface IHTTP.MiddlewareDescriptor
Middleware info container

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.MiddlewareDescriptor.cs#L11)
```csharp title="Declaration"
public interface IHTTP.MiddlewareDescriptor
```
## Properties
### UseParams
Is true when this path is custom that support params e.g.: /root/{folder}
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.MiddlewareDescriptor.cs#L16)
```csharp title="Declaration"
bool UseParams { get; }
```
### Path
Path e.g.: /root/home
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.MiddlewareDescriptor.cs#L21)
```csharp title="Declaration"
string Path { get; }
```
### Callback
Handler callback
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.MiddlewareDescriptor.cs#L26)
```csharp title="Declaration"
Action<IHTTP.ServerRequest, IHTTP.ServerResponse, Action> Callback { get; }
```
