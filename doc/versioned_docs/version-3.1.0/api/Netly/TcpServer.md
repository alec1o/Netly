---
title: Class TcpServer
sidebar_label: TcpServer
---
# Class TcpServer


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L12)
```csharp title="Declaration"
public class TcpServer : Server<TcpClient>
```
**Inheritance:** `System.Object` -> [Netly.Abstract.Server&lt;T&gt;](../Netly.Abstract/Server`T`)

## Properties
### IsEncrypted

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L14)
```csharp title="Declaration"
public bool IsEncrypted { get; }
```
### EncryptionProtocol

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L15)
```csharp title="Declaration"
public SslProtocols EncryptionProtocol { get; }
```
## Methods
### Open(Host, int)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L41)
```csharp title="Declaration"
public override void Open(Host host, int backlog)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| [Netly.Core.Host](../Netly.Core/Host) | *host* |
| `System.Int32` | *backlog* |

### IsConnected()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L81)
```csharp title="Declaration"
protected override bool IsConnected()
```

##### Returns

`System.Boolean`
### UseEncryption(byte[], string, SslProtocols)

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L88)
```csharp title="Declaration"
public void UseEncryption(byte[] pfxCertificate, string pfxPassword, SslProtocols encryptionProtocol)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Byte[]` | *pfxCertificate* |
| `System.String` | *pfxPassword* |
| `System.Security.Authentication.SslProtocols` | *encryptionProtocol* |

### AcceptOrReceive()

###### [View Source](https://github.com/alec1o/netly/blob/main/src/Tcp/TcpServers.cs#L106)
```csharp title="Declaration"
protected override void AcceptOrReceive()
```
