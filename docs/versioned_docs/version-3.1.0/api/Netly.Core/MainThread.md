---
title: Class MainThread
sidebar_label: MainThread
description: "Netly: MainThread"
---
# Class MainThread
Netly: MainThread

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MainThread.cs#L9)
```csharp title="Declaration"
public static class MainThread
```
## Properties
### Automatic
Automatic clean callbacks
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MainThread.cs#L17)
```csharp title="Declaration"
public static bool Automatic { get; set; }
```
## Methods
### Add(Action)
Add callback to execute on (Main/Own)Thread
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MainThread.cs#L23)
```csharp title="Declaration"
public static void Add(Action callback)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action` | *callback* | callback |

### Clean()
Use to clean/publish callbacks 

 
WARNING: only if "Automatic == false"
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Core/MainThread.cs#L44)
```csharp title="Declaration"
public static void Clean()
```
