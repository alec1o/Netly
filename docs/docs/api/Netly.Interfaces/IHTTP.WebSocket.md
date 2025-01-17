---
title: Interface IHTTP.WebSocket
sidebar_label: IHTTP.WebSocket
---
# Interface IHTTP.WebSocket


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L8)
```csharp title="Declaration"
public interface IHTTP.WebSocket
```
## Properties
### ServerRequest
Connection request
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L13)
```csharp title="Declaration"
IHTTP.ServerRequest ServerRequest { get; }
```
### Headers
Connection Headers
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L18)
```csharp title="Declaration"
Dictionary<string, string> Headers { get; }
```
### IsOpened
Return true if connection is opened
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L23)
```csharp title="Declaration"
bool IsOpened { get; }
```
### Host
Client Uri
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L28)
```csharp title="Declaration"
Uri Host { get; }
```
### On
Event Handler
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L33)
```csharp title="Declaration"
IHTTP.WebSocketOn On { get; }
```
### To
Event Creator
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.WebSocket.cs#L38)
```csharp title="Declaration"
IHTTP.WebSocketTo To { get; }
```
