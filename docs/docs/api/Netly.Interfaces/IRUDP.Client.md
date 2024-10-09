---
title: Interface IRUDP.Client
sidebar_label: IRUDP.Client
description: "RUDP Client instance (interface)"
---
# Interface IRUDP.Client
RUDP Client instance (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L8)
```csharp title="Declaration"
public interface IRUDP.Client
```
## Properties
### Id
Client ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L13)
```csharp title="Declaration"
string Id { get; }
```
### Host
Remote Host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L18)
```csharp title="Declaration"
Host Host { get; }
```
### IsOpened
Is Opened? (return true if connected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L23)
```csharp title="Declaration"
bool IsOpened { get; }
```
### HandshakeTimeout
Connection handshake timeout. [is milliseconds &gt;= 1000]


NOTE: (default value is 5000ms or 5s;
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L29)
```csharp title="Declaration"
int HandshakeTimeout { get; set; }
```
### NoResponseTimeout
Timeout that a connection can remain unresponsive, after the timeout the connection will be closed. [is milliseconds &gt;= 2000]


NOTE: (default value is 5000ms or 5s)


NOTE: Works after handshake successful
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L36)
```csharp title="Declaration"
int NoResponseTimeout { get; set; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L41)
```csharp title="Declaration"
IRUDP.ClientTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/interfaces/IRUDP.Client.cs#L46)
```csharp title="Declaration"
IRUDP.ClientOn On { get; }
```
