---
title: Interface IRUDP.Server
sidebar_label: IRUDP.Server
description: "RUDP Server (interface)"
---
# Interface IRUDP.Server
RUDP Server (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L8)
```csharp title="Declaration"
public interface IRUDP.Server
```
## Properties
### Id
Server ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L13)
```csharp title="Declaration"
string Id { get; }
```
### HandshakeTimeout
Server connection handshake timeout. [is milliseconds &gt;= 1000]


NOTE: (default value is 5000ms or 5s;
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L19)
```csharp title="Declaration"
int HandshakeTimeout { get; set; }
```
### NoResponseTimeout
Timeout that a client connection can remain unresponsive, after the timeout the connection will be closed. [is milliseconds &gt;= 2000]


NOTE: (default value is 5000ms or 5s)


NOTE: Works after handshake successful
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L26)
```csharp title="Declaration"
int NoResponseTimeout { get; set; }
```
### Host
Server host (bind endpoint)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L31)
```csharp title="Declaration"
Host Host { get; }
```
### IsOpened
Is Opened? (true if is bind)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L36)
```csharp title="Declaration"
bool IsOpened { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L41)
```csharp title="Declaration"
IRUDP.ServerTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L46)
```csharp title="Declaration"
IRUDP.ServerOn On { get; }
```
### Clients
Collections of connected client
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Server.cs#L51)
```csharp title="Declaration"
IRUDP.Client[] Clients { get; }
```
