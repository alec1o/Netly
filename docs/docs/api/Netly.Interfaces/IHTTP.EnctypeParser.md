---
title: Interface IHTTP.EnctypeParser
sidebar_label: IHTTP.EnctypeParser
---
# Interface IHTTP.EnctypeParser


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.EnctypeParser.cs#L5)
```csharp title="Declaration"
public interface IHTTP.EnctypeParser
```
## Properties
### IsValid

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.EnctypeParser.cs#L7)
```csharp title="Declaration"
bool IsValid { get; }
```
### Keys

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.EnctypeParser.cs#L8)
```csharp title="Declaration"
string[] Keys { get; }
```
### this[string]

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.EnctypeParser.cs#L9)
```csharp title="Declaration"
IHTTP.EnctypeObject this[string key] { get; }
```
