---
title: Class TcpClient
sidebar_label: TcpClient
---
# Class TcpClient


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L11)
```csharp title="Declaration"
public class TcpClient : Client
```
**Inheritance:** `System.Object` -> [Netly.Abstract.Client](../Netly.Abstract/Client)

## Properties
### IsEncrypted

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L14)
```csharp title="Declaration"
public bool IsEncrypted { get; }
```
## Methods
### UseEncryption(bool, Func&lt;object, X509Certificate, X509Chain, SslPolicyErrors, bool&gt;)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L64)
```csharp title="Declaration"
public void UseEncryption(bool enableEncryption, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> onValidation = null)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Boolean` | *enableEncryption* |
| `System.Func<System.Object,System.Security.Cryptography.X509Certificates.X509Certificate,System.Security.Cryptography.X509Certificates.X509Chain,System.Net.Security.SslPolicyErrors,System.Boolean>` | *onValidation* |

### Open(Host)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L79)
```csharp title="Declaration"
public override void Open(Host host)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* |

### ToData(byte[])

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L120)
```csharp title="Declaration"
public override void ToData(byte[] data)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *data* |

### Receive()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L198)
```csharp title="Declaration"
protected override void Receive()
```
### IsConnected()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpClient.cs#L281)
```csharp title="Declaration"
protected override bool IsConnected()
```

##### Returns

`System.Boolean`
