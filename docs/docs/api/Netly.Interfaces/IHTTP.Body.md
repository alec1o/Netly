---
title: Interface IHTTP.Body
sidebar_label: IHTTP.Body
---
# Interface IHTTP.Body


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L8)
```csharp title="Declaration"
public interface IHTTP.Body
```
## Properties
### Encoding
Body Encoding


&lt;i&gt;If not found in HTTP header UTF-8 is used by Default&lt;/i&gt;
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L14)
```csharp title="Declaration"
Encoding Encoding { get; }
```
### Enctype
Enctype type
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L19)
```csharp title="Declaration"
HTTP.Enctype Enctype { get; }
```
### Text
Text buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L24)
```csharp title="Declaration"
string Text { get; }
```
### Binary
Binary buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L29)
```csharp title="Declaration"
byte[] Binary { get; }
```
## Methods
### Parse&lt;T&gt;()
Parse HTTP Body using Detected Enctype
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L34)
```csharp title="Declaration"
T Parse<T>()
```

##### Returns

`<T>`
##### Type Parameters
* `T`
### Parse&lt;T&gt;(Enctype)
Parse HTTP Body using Custom Enctype
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L42)
```csharp title="Declaration"
T Parse<T>(HTTP.Enctype enctype)
```

##### Returns

`<T>`

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.HTTP.Enctype](../Netly/HTTP.Enctype) | *enctype* | Enctype Target |

##### Type Parameters
| Name | Description |
|:--- |:--- |
| `T` | Response Object |
### OnParse(Enctype, bool, Func&lt;Type, object&gt;)
Adding Enctype parser Method
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L53)
```csharp title="Declaration"
void OnParse(HTTP.Enctype enctype, bool replaceOnMatch, Func<Type, object> handler)
```

##### Parameters

| Type | Name | Description |
|:--- |:--- |:--- |
| [Netly.HTTP.Enctype](../Netly/HTTP.Enctype) | *enctype* | Enctype Target |
| `System.Boolean` | *replaceOnMatch* | &lt;i&gt;true:&lt;/i&gt; Replaces the existing &lt;i&gt;handler&lt;/i&gt; with this one if both target the same &lt;i&gt;Enctype&lt;/i&gt;.

&lt;i&gt;false:&lt;/i&gt; Uses this &lt;i&gt;handler&lt;/i&gt; only if no handler for the same &lt;i&gt;Enctype&lt;/i&gt; is set (does not replace an existing one). |
| `System.Func<System.Type,System.Object>` | *handler* | Target Enctype (Enctype that handler will solve) |

