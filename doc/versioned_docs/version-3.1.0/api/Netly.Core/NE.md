---
title: Class NE
sidebar_label: NE
description: "NE (Netly Encoding)"
---
# Class NE
NE (Netly Encoding)

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/NE.cs#L9)
```csharp title="Declaration"
public static class NE
```
## Properties
### Default
Is the default (generic) encoder used when encoding is not specified


Default value is: UTF8
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/NE.cs#L51)
```csharp title="Declaration"
public static NE.Mode Default { get; set; }
```
## Methods
### GetBytes(string)
Convert string to bytes (using NE.Default Mode)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/NE.cs#L59)
```csharp title="Declaration"
public static byte[] GetBytes(string value)
```

##### Returns

`System.Byte[]`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *value* | Value |

### GetString(byte[])
Convert bytes to string (using NE.Default Mode)
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/NE.cs#L69)
```csharp title="Declaration"
public static string GetString(byte[] value)
```

##### Returns

`System.String`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *value* | Value |

### GetString(byte[], Mode)
Convert bytes to string
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/NE.cs#L80)
```csharp title="Declaration"
public static string GetString(byte[] value, NE.Mode encode)
```

##### Returns

`System.String`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *value* | Value |
| [Netly.Core.NE.Mode](../Netly.Core/NE.Mode) | *encode* | Encoding protocol |

### GetBytes(string, Mode)
Convert value to bytes
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/NE.cs#L100)
```csharp title="Declaration"
public static byte[] GetBytes(string value, NE.Mode encode)
```

##### Returns

`System.Byte[]`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *value* | Value |
| [Netly.Core.NE.Mode](../Netly.Core/NE.Mode) | *encode* | Encoding protocol |

