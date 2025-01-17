---
title: Class RUDP.Server
sidebar_label: RUDP.Server
description: "RUDP Server implementation"
---
# Class RUDP.Server
RUDP Server implementation

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/RUDP.cs#L11)
```csharp title="Declaration"
public class RUDP.Server : IRUDP.Server
```
**Implements:**  
[Netly.Interfaces.IRUDP.Server](../Netly.Interfaces/IRUDP.Server)

## Properties
### Id
Server ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L20)
```csharp title="Declaration"
public string Id { get; }
```
### HandshakeTimeout
Server connection handshake timeout. [is milliseconds &gt;= 1000]


NOTE: (default value is 5000ms or 5s;
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L21)
```csharp title="Declaration"
public int HandshakeTimeout { get; set; }
```
### NoResponseTimeout
Timeout that a client connection can remain unresponsive, after the timeout the connection will be closed. [is milliseconds &gt;= 2000]


NOTE: (default value is 5000ms or 5s)


NOTE: Works after handshake successful
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L27)
```csharp title="Declaration"
public int NoResponseTimeout { get; set; }
```
### Host
Server host (bind endpoint)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L32)
```csharp title="Declaration"
public Host Host { get; }
```
### IsOpened
Is Opened? (true if is bind)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L33)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L34)
```csharp title="Declaration"
public IRUDP.ServerTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L35)
```csharp title="Declaration"
public IRUDP.ServerOn On { get; }
```
### Clients
Collections of connected client
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Server.cs#L36)
```csharp title="Declaration"
public IRUDP.Client[] Clients { get; }
```

## Implements

* [Netly.Interfaces.IRUDP.Server](../Netly.Interfaces/IRUDP.Server)
