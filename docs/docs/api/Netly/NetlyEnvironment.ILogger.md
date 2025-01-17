---
title: Interface NetlyEnvironment.ILogger
sidebar_label: NetlyEnvironment.ILogger
description: "Netly. Internal actions logger."
---
# Interface NetlyEnvironment.ILogger
Netly. Internal actions logger.

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.ILogger.cs#L10)
```csharp title="Declaration"
public interface NetlyEnvironment.ILogger
```
## Methods
### Create(string)
Create a log
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.ILogger.cs#L16)
```csharp title="Declaration"
void Create(string message)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *message* | Log message |

### Create(Exception)
Create a error log
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.ILogger.cs#L22)
```csharp title="Declaration"
void Create(Exception exception)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Exception` | *exception* | Exception object |

### On(Action&lt;string&gt;, bool)
Handle (regular log) callback
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.ILogger.cs#L29)
```csharp title="Declaration"
void On(Action<string> logCallback, bool useMainThread = false)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.String>` | *logCallback* | Callback |
| `System.Boolean` | *useMainThread* | Run callback on (Main Thread)? |

### On(Action&lt;Exception&gt;, bool)
Handle (exception log) callback
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/netly/interfaces/NetlyEnvironment.ILogger.cs#L37)
```csharp title="Declaration"
void On(Action<Exception> callback, bool useMainThread = false)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Action<System.Exception>` | *callback* | Callback |
| `System.Boolean` | *useMainThread* | Run callback on (Main Thread)? |

