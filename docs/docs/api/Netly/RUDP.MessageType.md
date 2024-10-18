---
title: Enum RUDP.MessageType
sidebar_label: RUDP.MessageType
---
# Enum RUDP.MessageType


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.MessageType.cs#L5)
```csharp title="Declaration"
public enum RUDP.MessageType : byte
```
## Fields
### Unreliable
Received Unordered and isn't reliable
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.MessageType.cs#L10)
```csharp title="Declaration"
Unreliable = 0
```
### Sequenced
Received Ordered and isn't reliable
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.MessageType.cs#L15)
```csharp title="Declaration"
Sequenced = 111
```
### Reliable
Received Ordered and is reliable
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/rudp/partials/RUDP.MessageType.cs#L20)
```csharp title="Declaration"
Reliable = 222
```
