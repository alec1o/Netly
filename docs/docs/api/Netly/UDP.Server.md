---
title: Class UDP.Server
sidebar_label: UDP.Server
description: "UDP Server implementation"
---
# Class UDP.Server
UDP Server implementation

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/UDP.cs#L11)
```csharp title="Declaration"
public class UDP.Server : IUDP.Server
```
**Implements:**  
[Netly.Interfaces.IUDP.Server](../Netly.Interfaces/IUDP.Server)

## Properties
### Id
Server ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Server.cs#L21)
```csharp title="Declaration"
public string Id { get; }
```
### Host
Server host (bind endpoint)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Server.cs#L22)
```csharp title="Declaration"
public Host Host { get; }
```
### IsOpened
Is Opened? (true if is bind)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Server.cs#L23)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Server.cs#L24)
```csharp title="Declaration"
public IUDP.ServerTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Server.cs#L25)
```csharp title="Declaration"
public IUDP.ServerOn On { get; }
```
### Clients
Collections of connected client
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Server.cs#L26)
```csharp title="Declaration"
public IUDP.Client[] Clients { get; }
```

## Implements

* [Netly.Interfaces.IUDP.Server](../Netly.Interfaces/IUDP.Server)
