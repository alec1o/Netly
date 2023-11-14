<h1 align="center"><a href="https://github.com/alec1o/netly">Netly</a></h1>

<h6 align="center"><sub>
powered by <a href="https://github.com/alec1o">ALEC1O</a><sub/>
</h6>

<div align="center">
  <img align="center" src="static/logo/netly-logo-3.png" width="128px" alt="netly logo">
</div>

##### About

> <sub> Netly is a powerful C# socket library that simplifies network communication. It supports TCP, SSL/TLS, and UDP protocols, making it ideal for building multiplayer games, chat applications, and more.</sub>

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

##### Sponsor and Supporter
> ###### Why Contribute to Netly?
  > <sub><code>Solve Real-World Challenges</code> Netly simplifies socket programming, making it accessible for developers. By contributing, you’ll directly impact how games, chat applications, and real-time systems communicate.</br></sub>  
  > <sub><code>Learn and Grow</code> Dive into the world of networking, encryption, and protocols. Gain practical experience by working on a versatile library used across platforms.</br></br></sub>
  > <sub><code>Be Part of Something Bigger</code> Netly is open source, and your contributions will benefit the entire community. Join a passionate group of developers who believe in collaboration and knowledge sharing.</br></br></sub>
  > <sub><code>Unique Contribution Model (No Money Involved)</code> ``No Financial Transactions`` Netly doesn’t accept monetary contributions. Instead, we value your time, expertise, and passion.</br></br></sub>
  > <sub><code>Code, Ideas, and Feedback</code> Whether you’re a seasoned developer or just starting out, your code, ideas, and feedback matter. Every line of code, every suggestion, and every bug report contributes to Netly’s growth.</sub>

</br>

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
| <sub>UDP ``client`` ``server``</sub> | <sub>TCP/UDP ``performance improvement``</sub>                      | <sub>UDP ``impl dgram connection using ping and timeout``</sub> | <sub>HTTP ``client`` ``server``</sub>      | 
|                                      |                                                                     | <sub>Message Framing ``memory and performance improve``</sub>   |                                            | 
|                                      |                                                                     | <sub>Message Framing ``new protocol``</sub>                     |                                            |
|                                      |                                                                     | <sub>Byter ``2.0``</sub>                                        |                                            | 
|                                      |                                                                     | <sub>Collaborative documentation ``docsify``</sub>              |                                            |

<br>

##### List of tested platforms

- <sub>[.NET](https://dotnet.microsoft.com) (SDK)</sub>
- <sub>[Mono](https://mono-project.com) (SDK)</sub>
- <sub>[Unity](https://unity.com) (Engine)</sub>

<br>

##### Feature

> <sub>Below are some missing features that are planned to be added in later versions.</sub><br>

- <sub>Websocket (v4)</sub>

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
$ dotnet build netly/ -c Release -o netly/bin/

# NOTE:
# Netly.dll require Byter.dll because is Netly dependency
# Netly.dll and Byter.dll have on build folder <netly-path>/bin/
```

<br>

##### Demo
- <sub>[TCP](#demo)</sub>
- <sub>[UDP](#demo)</sub>
- <sub>[HTTP](#demo)</sub>
- <sub>[WebSocket](#demo)</sub>

<br/>

##### TCP Demo
-   <sub>``class`` <strong>TcpClient</strong></sub>
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

-   <sub>``class`` <strong>TcpServer</strong></sub>    
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
