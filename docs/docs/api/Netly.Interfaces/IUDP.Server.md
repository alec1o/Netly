---
title: Interface IUDP.Server
sidebar_label: IUDP.Server
description: "UDP Server (interface)"
---
# Interface IUDP.Server
UDP Server (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L8)
```csharp title="Declaration"
public interface IUDP.Server
```
## Properties
### Id
Server ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L13)
```csharp title="Declaration"
string Id { get; }
```
### Host
Server host (bind endpoint)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L18)
```csharp title="Declaration"
Host Host { get; }
```
### IsOpened
Is Opened? (true if is bind)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L23)
```csharp title="Declaration"
bool IsOpened { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L28)
```csharp title="Declaration"
IUDP.ServerTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L33)
```csharp title="Declaration"
IUDP.ServerOn On { get; }
```
### Clients
Collections of connected client
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Server.cs#L38)
```csharp title="Declaration"
IUDP.Client[] Clients { get; }
```
