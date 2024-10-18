---
title: Class UDP.Client
sidebar_label: UDP.Client
description: "UDP Client implementation"
---
# Class UDP.Client
UDP Client implementation

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/UDP.cs#L18)
```csharp title="Declaration"
public class UDP.Client : IUDP.Client
```
**Implements:**  
[Netly.Interfaces.IUDP.Client](../Netly.Interfaces/IUDP.Client)

## Properties
### IsOpened
Is Opened? (return true if connected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Client.cs#L27)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### Host
Remote Host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Client.cs#L28)
```csharp title="Declaration"
public Host Host { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Client.cs#L29)
```csharp title="Declaration"
public IUDP.ClientTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Client.cs#L30)
```csharp title="Declaration"
public IUDP.ClientOn On { get; }
```
### Id
Client ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/partials/UDP.Client.cs#L31)
```csharp title="Declaration"
public string Id { get; }
```

## Implements

* [Netly.Interfaces.IUDP.Client](../Netly.Interfaces/IUDP.Client)
