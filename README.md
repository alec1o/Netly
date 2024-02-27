<table>
  <tr>
    <td>
      <p>WARNING: <a href="https://github.com/alec1o/Netly/discussions/36#discussion-6204441">NETLY NEXT RELEASE <code>4.X.X</code> ALERT!</a></p>
    </td>
  </tr>
</table>

<br>

<h1 align="center"><a href="https://github.com/alec1o/netly">Netly</a></h1>

<h6 align="center"><sub>
powered by <a href="https://github.com/alec1o">ALEC1O</a><sub/>
</h6>

<div align="center">
  <img align="center" src="static/logo/netly-logo-3.png" width="128px" alt="netly logo">
</div>

##### Project

> <sub>Get basic information about this project called [Netly](https://github.com/alec1o/Netly)</sub>

<table>
    <tr>
      <th align="center" valign="center"><sub><strong>Overview</strong></sub></th>
<td>
<br>

<sub>Netly is a powerful C# socket library that simplifies network communication. It supports HTTP, TCP, SSL/TLS, UDP
and WebSocket protocols, making it ideal for building multiplayer games, chat applications, and more.</sub>

<br>
</td>
    </tr>
    <tr>
      <th align="center" valign="center"><sub><strong>Link's</strong></sub></th>
<td>
<br>

<sub>
Repository: <a href="https://github.com/alec1o/Netly"><i>github.com/alec1o/netly</i></a>
<br>
Documentation: <a href="https://netly.docs.kezero.com"><i>netly.docs.kezero.com</i></a>
</sub>

<br>
<br>
</td>
    </tr>
    <tr>
      <th align="center" valign="center"><sub><strong>Install</strong></sub></th>
<td>
<br>

> <sub>Official publisher</sub>

| <sub>Nuget</sub>                                                    | <sub>Unity Asset Store</sub>                                                                     |
|---------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|
| <sub>Install on [Nuget](https://www.nuget.org/packages/Netly)</sub> | <sub>Install on [Asset Store ](https://assetstore.unity.com/packages/tools/network/225473)</sub> |

<br>
</td>
    </tr>
    <tr>
        <th align="center" valign="center"><sub><strong>Sponsor</strong></sub></th>
<td>
<br>

<div>
    <a href="https://www.jetbrains.com/community/opensource/"><img alt="JetBrains sponsor notice" src="/static/JetBrains%20sponsor.png" width="400px" /></a>
</div>

<br>
</td>
    </tr>
    <tr>
        <th align="center" valign="center"><sup><strong>Supporter</strong></sup></th>
<td>
<br>

<h6>Why Contribute to Netly</h6>

> <sub>Solve Real-World Challenges: Netly simplifies socket programming, making it accessible for developers. By
> contributing, you’ll directly impact how games, chat applications, and real-time systems communicate.</sub>

> <sub>Learn and Grow: Dive into the world of networking, encryption, and protocols. Gain practical experience by
> working on a versatile library used across platforms.</sub>

> <sub>Be Part of Something Bigger: Netly is open source, and your contributions will benefit the entire community. Join
> a passionate group of developers who believe in collaboration and knowledge sharing.</sub>

> <sub>Code, Ideas, and Feedback: Whether you’re a seasoned developer or just starting out, your code, ideas, and
> feedback matter. Every line of code, every suggestion, and every bug report contributes to Netly’s growth.</sub>

<br>
</td>
    </tr>
</table>

<br>

##### Examples

> <sub>Code highlights</sub>

<table>
    <tr>
      <th><sub><strong>Protocol</strong></sub></th>
      <th><sub><strong>Client</strong></sub></th>
      <th><sub><strong>Server</strong></sub></th>
    </tr>
    <tr>
      <th valign="center"><sub><strong>TCP</strong></sub></th>
<td valign="top">
<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp
using Netly;

TCP.Client client = new TCP.Client(framing: true);

#if CALLBACKS

client.On.Open(() =>
{   
    printf("connection opened");
});

client.On.Close(() =>
{
    printf("connetion closed");
});

client.On.Error((exception) =>
{
    printf("connection erro on open");
});

client.On.Data((bytes) =>
{
    printf("connection receive a raw data");
});

client.On.Event((name, data) =>
{
    printf("connection receive a event");
});

client.On.Modify((socket) =>
{
    printf("called before try open connection.");
});

client.On.Encryption((certificate, chain, errors) =>
{
    // Only if client.IsEncrypted is enabled
    printf("validate ssl/tls certificate");
    // return true if certificate is valid
    return true;
});

#endif

#if FUNCTIONS

// open connection if closed
client.To.Open(new Host("127.0.0.1", 8080));

// close connection if opened
client.To.Close();

// send raw data if connected
client.To.Data(new byte[2] { 128, 255 });
client.To.Data("hello world", NE.Encoding.UTF8);

// send event if connected
client.To.Event("name", new byte[2] { 128, 255 });
client.To.Event("name", "hello world", NE.Encoding.UTF8); 

// enable encryption (must call before client.To.Open)
client.To.Encryption(true); 

#endif
```

</details>
</td>
<td valign="top">
<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>


```csharp
using Netly;

TCP.Server server = new TCP.Server(framing: true);

#if CALLBACKS

server.On.Open(() =>
{   
    printf("connection opened");
});

server.On.Close(() =>
{
    printf("connection closed");
});

server.On.Error((exception) =>
{
    printf("connection error on open");
});

server.On.Accept((client) =>
{
    client.On.Open(() =>
    {
        printf("client connected");
    });
    
    client.On.Data((bytes) =>
    {
        printf("client receive a raw data");
    });
    
    client.On.Event((name, bytes) =>
    {
        printf("client receive a event");
    });
    
    client.On.Close(() =>
    {
        printf("client disconnected");
    });
});

server.On.Modify((socket) =>
{
    printf("called before try open connection.");
});

#endif

#if FUNCTIONS

// open connection
server.To.Open(new Host("1.1.1.1", 1111)); 

// close connection
server.To.Close();

// enable encryption support (must called before server.To.Open)
server.To.Encryption(enable: true, @mypfx, @mypfxpassword, SslProtocols.Tls12);

#endif
```
</details>
</td>
    </tr>
    <tr>
        <th><sub><strong>UDP</strong></sub></th>
<td valign="top">


<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp
using Netly;

UDP.Client client = new UDP.Client(useConnection: true, timeout: 15000);

#if CALLBACKS

client.On.Open(() =>
{
    printf("connection opened");
});

client.On.Close(() =>
{
    printf("connection closed");
});

client.On.Error((exception) =>
{
    printf("connection error on open");
});

client.On.Data((bytes) =>
{
    printf("connection received a raw data");
});

client.On.Event((name, eventBytes) =>
{
    printf("connection received a event");
});

client.On.Modify((socket) =>
{
   printf("called before try open connection.");
});

#endif

#if FUNCTIONS
    
// open connection if closed
client.To.Open(new Host("127.0.0.1", 8080));

// close connection if opened
client.To.Close();

// send raw data if connected
client.To.Data(new byte[2] { 128, 255 });
client.To.Data("hello world", NE.Encoding.UTF8);

// send event if connected
client.To.Event("name", new byte[2] { 128, 255 });
client.To.Event("name", "hello world", NE.Encoding.UTF8); 

#endif
```

</details>
</td>
<td valign="top">


<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp
using Netly;

UDP.Server server = new UDP.Server(useConnection: true, timeout: 15000);

#if CALLBACKS

server.On.Open(() =>
{
    printf("connection opened");
});

server.On.Close(() =>
{
    printf("connection closed");
});

server.On.Error((exception) =>
{
    printf("connection error on open");
});

server.On.Accept((client) =>
{
    client.On.Open(() =>
    {
        printf("client connected");
    });
    
    client.On.Close(() =>
    {
        // Only if use connection is enabled.
        printf("client disconnected");
    });
    
    client.On.Data((bytes) =>
    {
        printf("client received a raw data");
    });
    
    client.On.Event((name, bytes) =>
    {
        printf("client received a event");
    });
});

#endif

#if FUNCTIONS
    
// open connection
server.To.Open(new Host("127.0.0.1", 8080));

// close connection
server.To.Close();

// send raw data
server.To.Data(new byte[2] { 128, 255 });
server.To.Data("hello world", NE.Encoding.UTF8);

// send event
server.To.Event("name", new byte[2] { 128, 255 });
server.To.Event("name", "hello world", NE.Encoding.UTF8); 

#endif
```

</details>
</td>
    </tr>
    <tr>
        <th><sub><strong>HTTP</strong></sub></th>
<td valign="top">


<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp

```

</details>
</td>
<td valign="top">


<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp

```

</details>
</td>
    </tr>
    <tr>
        <th><sub><strong>WebSocket</strong></sub></th>
<td valign="top">


<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp

```

</details>
</td>
<td valign="top">


<details>
<summary>📄 <strong><sup><sub>Code </sub></sup></strong></summary>

```csharp

```

</details>
</td>
    </tr>
</table>

<br>

##### Versions

> <sub>Notable changes</sub>

| <sub>Version</sub> | <sub>Status</sub>      |                                                                               |                                                   |                                                                                                    |                                                                    |                                                                                       |
|--------------------|------------------------|-------------------------------------------------------------------------------|---------------------------------------------------|----------------------------------------------------------------------------------------------------|--------------------------------------------------------------------|---------------------------------------------------------------------------------------|
| <sub>4.x.x</sub>   | <sub>Development</sub> | <sub>HTTP client and server support</sub>                                     | <sub>WebSocket client and server</sub>            | <sub>Syntax and internal improvement</sub>                                                         | <sub>Code XML comments improvement</sub>                           | <sub>Documentation improvement by [DocFx](https://github.com/dotnet/docfx)</sub>      |
| <sub>3.x.x</sub>   | <sub>Stable</sub>      | <sub>TCP with TLS/SSL support</sub>                                           | <sub>UDP with connection (timeout response)</sub> | <sub>New [Message Framing](https://bit.ly/message-framing) protocol and performance increase</sub> | <sub>Update for [Byter 2.0](https://github.com/alec1o/Byter)</sub> | <sub>[Docsify](https://github.com/docsifyjs/docsify) as documentation framework</sub> |
| <sub>2.x.x</sub>   | <sub>Legacy</sub>      | <sub>TCP with [Message Framing](https://bit.ly/message-framing) support</sub> | <sub>TCP and UDP performance increase</sub>       |                                                                                                    |                                                                    |                                                                                       |
| <sub>1.x.x</sub>   | <sub>Legacy</sub>      | <sub>TCP support</sub>                                                        | <sub>UDP Support</sub>                            |                                                                                                    |                                                                    |                                                                                       |

<br>

##### Integrations

> <sub>Technical descriptions about integrations</sub>

<table>
    <tr valign="top" align="left">
        <th><sub>List of tested platforms</sub></th>
<td valign="top" align="left">
<br>

- <sub>[.NET](https://dotnet.microsoft.com) (SDK)</sub>
- <sub>[Mono](https://mono-project.com) (SDK)</sub>
- <sub>[Unity](https://unity.com) (Engine)</sub>
- <sub>[Operating system](https://en.wikipedia.org/wiki/Operating_system) (OS)</sub>
    - <sub>Linux</sub>
    - <sub>Windows</sub>
    - <sub>Android</sub>
    - <sub>iOS and macOS</sub><br><br>
    - <sub><strong>Notice</strong>: <i>This library might run on all devices. If it doesn't work on any device, it
      should be considered a bug and reported.<i><sub>

<br>
</td>
    </tr>
    <tr valign="top" align="left">
        <th><sub>Dependencies</sub></th>
<td valign="top" align="left">
<br>

- <sub>[Byter](https://github.com/alec1o/Byter)</sub>

<br>
</td>
    </tr>
    <tr valign="top" align="left">
        <th><sub>Build</sub></th>
<td valign="top" align="left">
<br>

> ###### Build dependencies

- <sub>[Git](http://git-scm.com/)</sub>
- <sub>[.NET](http://dot.net)</sub>

> ###### Build step-by-step

```rb
# 1. clone project
$ git clone "https://github.com/alec1o/Netly" netly 

# 2. build project
$ dotnet build "netly/" -c Release -o "netly/bin/"

# NOTE:
# Netly.dll require Byter.dll because is Netly dependency
# Netly.dll and Byter.dll have on build folder <netly-path>/bin/
```

<br>
</td>
    </tr>
    <tr valign="top" align="left">
        <th><sub>Features</sub></th>
<td valign="top" align="left">
<br>

> <sub>Below are some missing features that are planned to be added in later versions.</sub><br>

- <sub>``N/A``</sub>

<br>
</td>
    </tr>
</table>