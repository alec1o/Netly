---
title: Interface IHTTP.ClientTo
sidebar_label: IHTTP.ClientTo
description: "HTTP.Client action creator container"
---
# Interface IHTTP.ClientTo
HTTP.Client action creator container

###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientTo.cs#L11)
```csharp title="Declaration"
public interface IHTTP.ClientTo
```
## Methods
### Open(string, string)
Create http fetch 


+ Only if(IsOpened==false)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientTo.cs#L19)
```csharp title="Declaration"
Task Open(string method, string url)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *method* | Http method |
| `System.String` | *url* | Fetch url |

### Open(string, string, byte[])
Create http fetch 


+ Only if(IsOpened==false)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientTo.cs#L28)
```csharp title="Declaration"
Task Open(string method, string url, byte[] body)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *method* | Http method |
| `System.String` | *url* | Fetch url |
| `System.Byte[]` | *body* | Request body |

### Open(string, string, string)
Create http fetch 


+ Only if(IsOpened==false)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientTo.cs#L37)
```csharp title="Declaration"
Task Open(string method, string url, string body)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *method* | Http method |
| `System.String` | *url* | Fetch url |
| `System.String` | *body* | Request body |

### Open(string, string, string, Encoding)
Create http fetch 


+ Only if(IsOpened==false)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientTo.cs#L47)
```csharp title="Declaration"
Task Open(string method, string url, string body, Encoding encoding)
```

##### Returns

`System.Threading.Tasks.Task`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *method* | Http method |
| `System.String` | *url* | Fetch url |
| `System.String` | *body* | Request body |
| `System.Text.Encoding` | *encoding* | Body encoding algorithm |

### Close()
Cancel opened operation
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ClientTo.cs#L53)
```csharp title="Declaration"
Task Close()
```

##### Returns

`System.Threading.Tasks.Task`
