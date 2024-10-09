---
title: Class Host
sidebar_label: Host
description: "Netly: Host (EndPoint Manager)"
---
# Class Host
Netly: Host (EndPoint Manager)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L9)
```csharp title="Declaration"
public class Host
```
## Properties
### Address
Return IPAddress
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L59)
```csharp title="Declaration"
public IPAddress Address { get; }
```
### Port
Return Port
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L64)
```csharp title="Declaration"
public int Port { get; }
```
### EndPoint
Return EndPoint
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L69)
```csharp title="Declaration"
public EndPoint EndPoint { get; }
```
### IPEndPoint
Return IPEndPoint
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L74)
```csharp title="Declaration"
public IPEndPoint IPEndPoint { get; }
```
### AddressFamily
Return AddressFamily
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L79)
```csharp title="Declaration"
public AddressFamily AddressFamily { get; }
```
## Fields
### Default
Return default Host instance: (0.0.0.0:0)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L15)
```csharp title="Declaration"
public static readonly Host Default
```
## Methods
### ToString()

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L81)
```csharp title="Declaration"
public override string ToString()
```

##### Returns

`System.String`
### Equals(object)
Compare Host. Check IP/Port
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L91)
```csharp title="Declaration"
public override bool Equals(object @object)
```

##### Returns

`System.Boolean`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Object` | *object* | Object |

### Equals(object, object)
Compare two (2) Host. Check IP/Port
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L102)
```csharp title="Declaration"
public static bool Equals(object objectA, object objectB)
```

##### Returns

`System.Boolean`: Return true if those object is Host and have same value
##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Object` | *objectA* | Object A |
| `System.Object` | *objectB* | Object B |

### GetHashCode()

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/Host.cs#L112)
```csharp title="Declaration"
public override int GetHashCode()
```

##### Returns

`System.Int32`
