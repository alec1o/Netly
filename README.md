<h1 align="center"><a href="https://github.com/alec1o/netly">Netly</a></h1>

<h6 align="center"><sub>
powered by <a href="https://github.com/alec1o">ALEC1O</a><sub/>
</h6>

<div align="center">
  <img align="center" src="static/logo/netly-logo-3.png" width="128px" alt="netly logo">
</div>

##### About

> <sub> Netly is a powerful C# socket library that simplifies network communication. It supports HTTP, TCP, SSL/TLS, UDP, RUDP and WebSocket protocols, making it ideal for building multiplayer games, chat applications, and more.</sub>

<br>

##### Documentation

> <sub>Netly docs ([netly.docs.kezero.com](https://netly.docs.kezero.com))</sub>

<br>

##### Install

> <sub>Official publisher</sub>

| <sub>Nuget</sub>                                                    | <sub>Unity Asset Store</sub>                                                                     |
|---------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|
| <sub>Install on [Nuget](https://www.nuget.org/packages/Netly)</sub> | <sub>Install on [Asset Store ](https://assetstore.unity.com/packages/tools/network/225473)</sub> |

<br>

##### Supporter
> ###### Why Contribute to Netly?
  > <sub><code>Solve Real-World Challenges</code> Netly simplifies socket programming, making it accessible for developers. By contributing, you’ll directly impact how games, chat applications, and real-time systems communicate.</br></sub>  
  > <sub><code>Learn and Grow</code> Dive into the world of networking, encryption, and protocols. Gain practical experience by working on a versatile library used across platforms.</br></br></sub>
  > <sub><code>Be Part of Something Bigger</code> Netly is open source, and your contributions will benefit the entire community. Join a passionate group of developers who believe in collaboration and knowledge sharing.</br></br></sub>
  > <sub><code>Code, Ideas, and Feedback</code> Whether you’re a seasoned developer or just starting out, your code, ideas, and feedback matter. Every line of code, every suggestion, and every bug report contributes to Netly’s growth.</sub>

</br>

##### Sponsor

<div>
  <a href="https://www.jetbrains.com/community/opensource/">
    <img alt="JetBrains sponsor notice" src="/static/JetBrains%20sponsor.png" width="400px" />
  </a>
</div>

<br>

##### Versions

> <sub>Notable changes</sub>

| <sub>v1 (old)</sub>                  | <sub>v2 (old)</sub>                                                 | <sub>v3 (stable)</sub>                                          | <sub>v4 (In development)</sub>             |
|--------------------------------------|---------------------------------------------------------------------|-----------------------------------------------------------------|--------------------------------------------|
| <sub>TCP ``client`` ``server``</sub> | <sub>TCP/IP [Message Framing](https://bit.ly/message-framing)</sub> | <sub>TLS/SSL ``client`` ``server``</sub>                        | <sub>WebSocket ``client`` ``server``</sub> |
| <sub>UDP ``client`` ``server``</sub> | <sub>TCP/UDP ``performance increase``</sub>                         | <sub>UDP ``connection from (ping/timeout)``</sub>               | <sub>HTTP ``client`` ``server``</sub>      | 
|                                      |                                                                     | <sub>Message Framing ``performance increase``</sub>             | <sub>TCP ``internal improvement``<sub/>    | 
|                                      |                                                                     | <sub>Message Framing ``new protocol``</sub>                     | <sub>Documentation ``improvement``</sub>      |
|                                      |                                                                     | <sub>Byter ``2.0``</sub>                                        | <sub>XML description ``improvement``<sub>     | 
|                                      |                                                                     | <sub>Collaborative documentation ``docsify``</sub>              |                                            |

<br>

##### List of tested platforms

- <sub>[.NET](https://dotnet.microsoft.com) (SDK)</sub>
- <sub>[Mono](https://mono-project.com) (SDK)</sub>
- <sub>[Unity](https://unity.com) (Engine)</sub>

<br>

##### Feature

> <sub>Below are some missing features that are planned to be added in later versions.</sub><br>

- <sub>``N/A``</sub>

<br>

##### Dependency

- <sub>[Byter](https://github.com/alec1o/Byter)</sub>

<br>

##### Build

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
