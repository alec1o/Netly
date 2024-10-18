---
title: Class HTTP.WebSocket
sidebar_label: HTTP.WebSocket
description: "WebSocket Client"
---
# Class HTTP.WebSocket
WebSocket Client

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L9)
```csharp title="Declaration"
public class HTTP.WebSocket : IHTTP.WebSocket
```
**Implements:**  
[Netly.Interfaces.IHTTP.WebSocket](../Netly.Interfaces/IHTTP.WebSocket)

## Properties
### ServerRequest
Connection request
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L32)
```csharp title="Declaration"
public IHTTP.ServerRequest ServerRequest { get; }
```
### Headers
Connection Headers
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L33)
```csharp title="Declaration"
public Dictionary<string, string> Headers { get; }
```
### Host
Client Uri
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L34)
```csharp title="Declaration"
public Uri Host { get; }
```
### IsOpened
Return true if connection is opened
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L35)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### On
Event Handler
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L36)
```csharp title="Declaration"
public IHTTP.WebSocketOn On { get; }
```
### To
Event Creator
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/partials/Websocket/HTTP.WebSocket.cs#L37)
```csharp title="Declaration"
public IHTTP.WebSocketTo To { get; }
```

## Implements

* [Netly.Interfaces.IHTTP.WebSocket](../Netly.Interfaces/IHTTP.WebSocket)
