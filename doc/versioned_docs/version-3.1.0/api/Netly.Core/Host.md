---
title: Class Host
sidebar_label: Host
description: "Netly: Host (EndPoint Manager)"
---
# Class Host
Netly: Host (EndPoint Manager)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L10)
```csharp title="Declaration"
public class Host
```
## Properties
### Address
Return IPAddress
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L21)
```csharp title="Declaration"
public IPAddress Address { get; }
```
### Port
Return Port
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L26)
```csharp title="Declaration"
public int Port { get; }
```
### EndPoint
Return EndPoint
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L31)
```csharp title="Declaration"
public EndPoint EndPoint { get; }
```
### IPEndPoint
Return IPEndPoint
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L36)
```csharp title="Declaration"
public IPEndPoint IPEndPoint { get; }
```
### AddressFamily
Return AddressFamily
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L41)
```csharp title="Declaration"
public AddressFamily AddressFamily { get; }
```
## Fields
### Default
Return default Host instance: (0.0.0.0:0)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L16)
```csharp title="Declaration"
public static readonly Host Default
```
## Methods
### ToString()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/Host.cs#L84)
```csharp title="Declaration"
public override string ToString()
```

##### Returns

`System.String`
