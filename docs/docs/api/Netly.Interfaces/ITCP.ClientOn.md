---
title: Interface ITCP.ClientOn
sidebar_label: ITCP.ClientOn
---
# Interface ITCP.ClientOn


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientOn.cs#L10)
```csharp title="Declaration"
public interface ITCP.ClientOn : IOn<Socket>
```
## Methods
### Data(Action&lt;byte[]&gt;)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientOn.cs#L12)
```csharp title="Declaration"
void Data(Action<byte[]> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.Byte[]>` | *callback* |

### Event(Action&lt;string, byte[]&gt;)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientOn.cs#L13)
```csharp title="Declaration"
void Event(Action<string, byte[]> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Action<System.String,System.Byte[]>` | *callback* |

### Encryption(Func&lt;X509Certificate, X509Chain, SslPolicyErrors, bool&gt;)

###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/tcp/interfaces/ITCP.ClientOn.cs#L14)
```csharp title="Declaration"
void Encryption(Func<X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
```

##### Parameters

| Type | Name |
|:--- |:--- |
| `System.Func<System.Security.Cryptography.X509Certificates.X509Certificate,System.Security.Cryptography.X509Certificates.X509Chain,System.Net.Security.SslPolicyErrors,System.Boolean>` | *callback* |

