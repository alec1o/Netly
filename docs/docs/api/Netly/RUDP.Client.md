---
title: Class RUDP.Client
sidebar_label: RUDP.Client
description: "RUDP Client implementation"
---
# Class RUDP.Client
RUDP Client implementation

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/RUDP.cs#L18)
```csharp title="Declaration"
public class RUDP.Client : IRUDP.Client
```
**Implements:**  
[Netly.Interfaces.IRUDP.Client](../Netly.Interfaces/IRUDP.Client)

## Properties
### IsOpened
Is Opened? (return true if connected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L25)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### Host
Remote Host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L26)
```csharp title="Declaration"
public Host Host { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L27)
```csharp title="Declaration"
public IRUDP.ClientTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L28)
```csharp title="Declaration"
public IRUDP.ClientOn On { get; }
```
### Id
Client ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L29)
```csharp title="Declaration"
public string Id { get; }
```
### HandshakeTimeout
Connection handshake timeout. [is milliseconds &gt;= 1000]


NOTE: (default value is 5000ms or 5s;
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L31)
```csharp title="Declaration"
public int HandshakeTimeout { get; set; }
```
### NoResponseTimeout
Timeout that a connection can remain unresponsive, after the timeout the connection will be closed. [is milliseconds &gt;= 2000]


NOTE: (default value is 5000ms or 5s)


NOTE: Works after handshake successful
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.Client.cs#L37)
```csharp title="Declaration"
public int NoResponseTimeout { get; set; }
```

## Implements

* [Netly.Interfaces.IRUDP.Client](../Netly.Interfaces/IRUDP.Client)
