---
title: Interface IHTTP.ServerResponse
sidebar_label: IHTTP.ServerResponse
---
# Interface IHTTP.ServerResponse


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L10)
```csharp title="Declaration"
public interface IHTTP.ServerResponse
```
## Properties
### NativeResponse
Native Response Object
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L15)
```csharp title="Declaration"
HttpListenerResponse NativeResponse { get; }
```
### WebSocketContext
WebSocket Context Object (valid only when 'ServerRequest.IsWebSocket is &lt;i&gt;true&lt;/i&gt;', otherwise is 'null')
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L20)
```csharp title="Declaration"
HttpListenerWebSocketContext WebSocketContext { get; }
```
### Headers
Response Headers
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L25)
```csharp title="Declaration"
Dictionary<string, string> Headers { get; }
```
### Cookies
Response Cookies
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L30)
```csharp title="Declaration"
Cookie[] Cookies { get; }
```
### Encoding
Response encoding
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L35)
```csharp title="Declaration"
Encoding Encoding { get; set; }
```
### IsOpened
Return true if response connection is opened
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L40)
```csharp title="Declaration"
bool IsOpened { get; }
```
## Methods
### Send(int)
Send response data (empty)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L46)
```csharp title="Declaration"
void Send(int statusCode)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Int32` | *statusCode* | http status code |

### Write(byte[])
Write data on final buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L52)
```csharp title="Declaration"
void Write(byte[] byteBuffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Byte[]` | *byteBuffer* | data |

### Write(string)
Write data on final buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L58)
```csharp title="Declaration"
void Write(string textBuffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *textBuffer* | data |

### Send(int, string)
Send response data (string)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L65)
```csharp title="Declaration"
void Send(int statusCode, string textBuffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Int32` | *statusCode* | http status code |
| `System.String` | *textBuffer* | response data |

### Send(int, byte[])
Send response data (bytes)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L72)
```csharp title="Declaration"
void Send(int statusCode, byte[] byteBuffer)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Int32` | *statusCode* | http status code |
| `System.Byte[]` | *byteBuffer* | response data |

### Redirect(string)
Redirect connection for an url.

Using
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L78)
```csharp title="Declaration"
void Redirect(string url)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.String` | *url* | redirect location |

### Redirect(int, string)
Redirect connection for an url.

Using
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L85)
```csharp title="Declaration"
void Redirect(int redirectCode, string url)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| `System.Int32` | *redirectCode* | redirect http code |
| `System.String` | *url* | redirect location |

### Close()
Close connection
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.ServerResponse.cs#L90)
```csharp title="Declaration"
void Close()
```
