---
title: Class TCP.Server
sidebar_label: TCP.Server
description: "TCP Server"
---
# Class TCP.Server
TCP Server

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/TCP.cs#L11)
```csharp title="Declaration"
public class TCP.Server
```
## Properties
### Id

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L28)
```csharp title="Declaration"
public string Id { get; }
```
### Host

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L30)
```csharp title="Declaration"
public Host Host { get; }
```
### IsOpened

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L31)
```csharp title="Declaration"
public bool IsOpened { get; }
```
### IsFraming

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L32)
```csharp title="Declaration"
public bool IsFraming { get; }
```
### Certificate

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L34)
```csharp title="Declaration"
public X509Certificate Certificate { get; }
```
### EncryptionProtocol

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L35)
```csharp title="Declaration"
public SslProtocols EncryptionProtocol { get; }
```
### IsEncrypted

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L36)
```csharp title="Declaration"
public bool IsEncrypted { get; }
```
### To

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L37)
```csharp title="Declaration"
public ITCP.ServerTo To { get; }
```
### On

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L38)
```csharp title="Declaration"
public ITCP.ServerOn On { get; }
```
### Clients

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/partials/TCP.Server.cs#L39)
```csharp title="Declaration"
public ITCP.Client[] Clients { get; }
```
