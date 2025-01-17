---
title: Interface NetlyEnvironment.IMainThread
sidebar_label: NetlyEnvironment.IMainThread
description: "Netly. Internal actions logger."
---
# Interface NetlyEnvironment.IMainThread
Netly. Internal actions logger.

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.IMainThread.cs#L10)
```csharp title="Declaration"
public interface NetlyEnvironment.IMainThread
```
## Properties
### IsAutomatic
Execute action automatically or on custom MainThread.


True: The actions will automatic executed with same thread that add action.


False: The actions will be executed with custom dispatch thread.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.IMainThread.cs#L17)
```csharp title="Declaration"
bool IsAutomatic { get; set; }
```
## Methods
### Add(Action)
Add action to be execution queue in main thread.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.IMainThread.cs#L23)
```csharp title="Declaration"
void Add(Action action)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action` | *action* |

### Dispatch()
Dispatch actions in queue.


 Note: Only works if (IsAutomatic is false), otherwise the actions will be dispatched automatically with same
thread that add that in queue.
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.IMainThread.cs#L30)
```csharp title="Declaration"
void Dispatch()
```
