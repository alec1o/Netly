---
title: Interface IUDP.Client
sidebar_label: IUDP.Client
description: "UDP Client instance (interface)"
---
# Interface IUDP.Client
UDP Client instance (interface)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Client.cs#L8)
```csharp title="Declaration"
public interface IUDP.Client
```
## Properties
### Id
Client ID (readonly)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Client.cs#L13)
```csharp title="Declaration"
string Id { get; }
```
### Host
Remote Host
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Client.cs#L18)
```csharp title="Declaration"
Host Host { get; }
```
### IsOpened
Is Opened? (return true if connected)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Client.cs#L23)
```csharp title="Declaration"
bool IsOpened { get; }
```
### To
Actions container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Client.cs#L28)
```csharp title="Declaration"
IUDP.ClientTo To { get; }
```
### On
Callbacks container
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/udp/interfaces/IUDP.Client.cs#L33)
```csharp title="Declaration"
IUDP.ClientOn On { get; }
```
