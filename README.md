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

<sub>Netly is a powerful C# socket library that simplifies network communication. It supports HTTP, TCP, SSL/TLS, UDP and WebSocket protocols, making it ideal for building multiplayer games, chat applications, and more.</sub>

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

> <sub>Solve Real-World Challenges: Netly simplifies socket programming, making it accessible for developers. By contributing, you’ll directly impact how games, chat applications, and real-time systems communicate.</sub>

> <sub>Learn and Grow: Dive into the world of networking, encryption, and protocols. Gain practical experience by working on a versatile library used across platforms.</sub>

> <sub>Be Part of Something Bigger: Netly is open source, and your contributions will benefit the entire community. Join a passionate group of developers who believe in collaboration and knowledge sharing.</sub>

> <sub>Code, Ideas, and Feedback: Whether you’re a seasoned developer or just starting out, your code, ideas, and feedback matter. Every line of code, every suggestion, and every bug report contributes to Netly’s growth.</sub>

<br>
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

<!-- information site -->

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





<br>

##### Demo

> ###### HTTP

<ul>

<details>
    <summary><sub><strong>HTTP Client</strong></sub></summary>

```csharp
using System;
using Netly;

var client = new HttpClient();

// error callback
client.OnError((exception) =>
{
    // request exception error. when connection doesn't open, null uri,...
});

// success callback
client.OnSuccess((request) =>
{
    // get status code
    int statusCode = request.StatusCode;
    // get server response as plain-text
    string bodyAsPlainText =  request.Body.PlainText;   
});  

// EXAMPLE:
    // set header
    client.Headers.Add("content-type", "multipart/form-data");
    // set url query
    client.Queries.Add("timeout", "1h");
      
    // create form data
    var body = new RequestBody(Enctype.Multipart);
    // set filename.
    body.Add("name", "Video.mp4");
    // set filebuffer.
    body.Add("file", new byte[]{ 1, 3, 4, 5, 6, 7, 8, 9, 0 });
    
    // set request body.
    client.Body = body;

// Don't block main thread, run on threadpolls.
// Send POST request.
client.Send("POST", new Uri("http://drive.kezero.com/upload?timeout=1h"));
```

</details>

<details>
    <summary><sub><strong>HTTP Server</strong></sub></summary>

```csharp
using System;
using Netly;

var server = new HttpServer();

server.OnOpen(() =>
{
    //  server started.
});

server.OnClose(() =>
{
    // server closed.
});

server.OnError((exception) =>
{
    // error on open server connection.
});

server.MapAll("/foo", (request, response) =>
{
    // receives request from all http methods.
    
    // EXAMPLE:
        // Send response for client.
        response.Send(200, $"Hello World. this http method is [{request.Method}]");
});

server.MapGet("/", (request, response) =>
{
    // received GET request at "/" path.
});

server.MapPost("/login", (request, response) =>
{
    // received POST request at "/login" path.

    // EXAMPLE:
        // request body on plain text.
        string text = request.Body.PlainText;  
        // get email from http form.
        string email = request.Body.Form.GetString("email");
        // get password from http from.
        string password = request.Body.Form.GetString("password");
        // get uploaded file from http form. (<form method="post" enctype="multipart/form-data">).
        byte[] picture = request.Body.Form.GetBytes("upload");  
});

server.Open(new Uri("http://localhost:8080"));
```

</details>

</ul>
<br>

> ###### TCP

<ul>

<details>
    <summary><sub><strong>Tcp Client</strong></sub></summary>

```csharp
  using Netly;
  using Netly.Core;
  
  var client = new TcpClient(framing: true);
  
  // Enable SSL/TLS (onValidate delegate is optional)
  client.UseEncryption(enableEncryption: true, onValidate: null);
  
  client.OnOpen(() => 
  {
      // client connected
  });
  
  client.OnClose(() =>
  {
      // client disconnected
  });
  
  client.OnError((Exception exception) =>
  {
      // connection close because: 1.Error on connecting, 2.Invalid framing data
  });
  
  client.OnData((byte[] data) =>
  {
      // raw data received
  });
  
  client.OnEvent((string name, byte[] data) =>
  {
      // event received (event use netly protocol) 
  });
  
  client.OnModify((Socket socket) =>
  {
      // you can modify socket, called before open connection
  });
  
  client.Open(new Host("127.0.0.1", 8080));
```

</details>

<details>
    <summary><sub><strong>Tcp Server</strong></sub></summary>

```csharp
using Netly;
using Netly.Core;

var server = new TcpServer(framing: true);

// Enable SSL/TLS
byte[] pfxCert = <DO_SOMETHING>;
string pfxPass = <DO_SOMETHING>;

server.UseEncryption(pfxCert, pfxPass, SslProtocols.Tls13); // TLS v1.3

server.OnOpen(() => 
{
    // server start listen
});

server.OnClose(() =>
{
    // server stop listen
});

server.OnError((Exception exception) =>
{
    // error on start listen (connecting)
});

server.OnData((TcpClient client, byte[] data) =>
{
    // a client receive raw data
});

server.OnEvent((TcpClient client, string name, byte[] data) =>
{
    // a client receive event (event use netly protocol)
});

server.OnEnter((TcpClient client) =>
{
    // a client connected on server
    
    client.OnClose(() =>
    {
        // alternative of: TcpServer.OnClose
    });
    
    client.OnData(() =>
    {
        // alternative of: TcpServer.OnData
    });
    
    client.OnEvent(() =>
    {
        // alternative of: TcpServer.OnEvent
    });
});

server.OnExit((TcpClient client) =>
{
    // a client disconnected from server
});

server.OnModify((Socket socket) =>
{
    // you can modify socket, called before listen and bind a port 
});

server.Open(new Host("127.0.0.1", 8080));
```

</details>

</ul>
<br>

> ###### UDP

<ul>

<details>
    <summary><sub><strong>Udp Client</strong></sub></summary>

```csharp
  using Netly;
  using Netly.Core;
  
  var client = new UdpClient(useConnection: true, timeout: 10000 /* 10s */);
        
  client.OnOpen(() => 
  {
      // client connected
  });
  
  client.OnClose(() =>
  {
      // client disconnected
  });
  
  client.OnError((Exception exception) =>
  {
      // connection close because: 1.Error on connecting, 2.Invalid framing data
  });
  
  client.OnData((byte[] data) =>
  {
      // raw data received
  });
  
  client.OnEvent((string name, byte[] data) =>
  {
      // event received (event use netly protocol) 
  });
  
  client.OnModify((Socket socket) =>
  {
      // you can modify socket, called before open connection
  });
  
  client.Open(new Host("127.0.0.1", 8080));
```

</details>

<details>
    <summary><sub><strong>Udp Server</strong></sub></summary>

```csharp
using Netly;
using Netly.Core;

var server = new UdpServer(useConnection: true, timeout: 15000 /* 15s */);

server.OnOpen(() => 
{
    // server start listen
});

server.OnClose(() =>
{
    // server stop listen
});

server.OnError((Exception exception) =>
{
    // error on start listen (connecting)
});

server.OnData((UdpClient client, byte[] data) =>
{
    // a client receive raw data
});

server.OnEvent((UdpClient client, string name, byte[] data) =>
{
    // a client receive event (event use netly protocol)
});

server.OnEnter((UdpClient client) =>
{
    // a client connected on server
    
    client.OnClose(() =>
    {
        // alternative of: TcpServer.OnClose
    });
    
    client.OnData(() =>
    {
        // alternative of: TcpServer.OnData
    });
    
    client.OnEvent(() =>
    {
        // alternative of: TcpServer.OnEvent
    });
});

server.OnExit((UdpClient client) =>
{
    // a client disconnected from server
});

server.OnModify((Socket socket) =>
{
    // you can modify socket, called before listen and bind a port 
});

server.Open(new Host("127.0.0.1", 8080));
```

</details>

</ul>
<br>

> ###### WebSocket

<ul>

<details>
    <summary><sub><strong>Websocket Client</strong></sub></summary>

```csharp
using System;
using Netly;
using Netly.Core;

var client = new WebsocketClient();

client.OnOpen(() =>
{
    // websocket client connected.
});

client.OnClose(() =>
{
    // websocket client disconnected.
});

client.OnError((exception) =>
{
    // error on connect to server.
});

client.OnData((bytes, bufferType) =>
{
    // websocket client received some data.
    // EXAMPLE:
        // send text data to server.
        client.ToData("hello world!");
        // send bynary data to server.
        client.ToData(new byte[]{ 1, 2, 3 });
});

client.OnEvent((name, bytes, bufferType) =>
{
    // websocket receives Netly event (Only Netly)
        // EXAMPLE:
        if (name == "client quit")
        {
            // send event to server
            client.ToEvent("goodbye", "Some data here");
            // close connection.
            client.Close();
        }
});

client.OnModify((ws) =>
{
    // modify socket
});

// open connection.
client.Open(new Uri("ws://localhost:3000/"));
```

</details>

<details>
    <summary><sub><strong>Websocket Server</strong></sub></summary>

```csharp
using System;
using Netly;

var server = new HttpServer();

server.OnOpen(() =>
{
    //  server started.
});

server.OnClose(() =>
{
    // server closed.
});

server.OnError((exception) =>
{
    // error on open server connection.
});

// create websocket echo server.
server.MapWebsocket("/echo", (request, client) =>
{
    client.OnData((bytes, bufferType) =>
    { 
        // echo data.
        client.ToData(bytes, bufferType);
    });

    client.OnEvent((name, bytes, bufferType) =>
    { 
        // echo event.
        client.ToEvent(name, bytes, bufferType);
    });
});

// websocket server run on "/chat" route.
server.MapWebsocket("/chat", (request, client) =>
{    
    // websocket client connected.
    
    // EXAMPLE:
        // send data to client.
        client.ToData("some data");
        // send event to client.
        client.ToEvent("hello_client", "some data");

    // receive data
    client.OnData((bytes, bufferType) =>
    {
        // websocket connected receive some data.
    });

    client.OnClose(() =>
    {
        // websocket client disconnected.
    });

    // receive event.
    client.OnEvent((name, bytes, bufferType) =>
    {
        // websocket connected receive some event.
    });
});

server.Open(new Uri("http://localhost:8080"));
```

</details>

</ul>
<br>
