---
title: Class KeyValueContainer
sidebar_label: KeyValueContainer
description: "KeyValue Element Container"
---
# Class KeyValueContainer
KeyValue Element Container

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L9)
```csharp title="Declaration"
public class KeyValueContainer
```
## Properties
### AllKeyValue
Return all KV elements
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L17)
```csharp title="Declaration"
public KeyValue<string, string>[] AllKeyValue { get; }
```
### Count
Return the size (count) of all KV elements
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L22)
```csharp title="Declaration"
public int Count { get; }
```
### Length
Return the size (length) of all KV elements
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L27)
```csharp title="Declaration"
public int Length { get; }
```
## Methods
### GetLength()
Return the amount of all KV element
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L42)
```csharp title="Declaration"
public int GetLength()
```

##### Returns

`System.Int32`
### GetAllKeyValue()
Return all KV element
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L54)
```csharp title="Declaration"
public KeyValue<string, string>[] GetAllKeyValue()
```

##### Returns

`Netly.Core.KeyValue<System.String,System.String>[]`
### Add(string, string)
Add a KV element in container
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L67)
```csharp title="Declaration"
public void Add(string name, string value)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | name (Key) |
| `System.String` | *value* | data (Value) |

### Remove(string)
Remove a KV element if exist
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L82)
```csharp title="Declaration"
public void Remove(string name)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | name (Key) |

### Get(string)
Return value (data) of some KV element.
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/KeyValueContainer.cs#L104)
```csharp title="Declaration"
public string Get(string name)
```

##### Returns

`System.String`: It return value (data) of a name (key). 

 Warning it return Empty (null) when data not found or KV element value (data) is Empty (White Space)
##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *name* | name (Key) |

