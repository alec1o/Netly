##### About

> Netly is a flexible socket library built on c-sharp. It is compatible with (Android, iOS, Linux,
> Windows...)

##### Documentation

> Netly docs ([HERE](https://netly.docs.kezero.com))

##### Install

> Official publisher

| Nuget                                                    | Unity Asset Store                                                                     |
|----------------------------------------------------------|---------------------------------------------------------------------------------------|
| Install on [Nuget](https://www.nuget.org/packages/Netly) | Install on [Asset Store ](https://assetstore.unity.com/packages/tools/network/225473) |

##### Sponsor and Supporter

> Well, this project is open source and only development can be supported by suggestions for improvements, bug
> reports or the like. (for those who want to financially support this resource is not available at this time)

##### Versions

> Notable changes

| v1 (old)            | v2 (old)                                                 | v3 (stable)                                        | v4 (nonexistent)                |
|---------------------|----------------------------------------------------------|----------------------------------------------------|---------------------------------|
| TCP (client/server) | TCP/IP [Message Framing](https://bit.ly/message-framing) | TLS/SSL (client/server)                            | Websocket (client/server)       |
| UDP (client/server) | TCP/UDP performance improvement                          | Include docs/sample (SSL/TLS)                      | Include docs/sample (Websocket) | 
|                     |                                                          | ``Message Framing`` memory and performance improve | HTTP (client/server)            | 
|                     |                                                          | ``Message Framing`` new protocol                   | Include docs/sample (HTTP)      |
|                     |                                                          | ``UDP`` impl connection with udp (ping/timeout)    |                                 | 
|                     |                                                          | Collaborative documentation ``docsify``            |                                 | 
|                     |                                                          | Byter ``2.0``                                      |                                 | 

##### List of tested platforms

- [.NET](https://dotnet.microsoft.com) (SDK)
- [Mono](https://mono-project.com) (SDK)
- [Unity](https://unity.com) (Engine)

##### Feature

> Below are some missing features that are planned to be added in later versions.

- Websocket (v4)

##### Dependency

- [Byter](https://github.com/alec1o/Byter)

##### Build

> ###### Build dependencies

- [Git](http://git-scm.com/)
- [.NET](http://dot.net)

> ###### Build step-by-step

  ```php
  # 1. clone repository 
  $ git clone "https://github.com/alec1o/netly" netly/

  # 2. build netly project
  $ dotnet build -C Release netly/
    # DLL_PATH: netly/src/bin/netstandard2.0/Netly.dll

  # 3. For use Netly.dll you need Byter.dll (a Netly dependecy)
  $ git clone "https://github.com/alec1o/byter" byter/

  # 4. build byter project
  $ dotnet build -C Release byter/
    # DLL_PATH: byter/src/bin/netstandard2.0/Byter.dll

  # WARNING: when use Netly.dll must include Byter.dll  
  ```

##### Demo

> TcpClient ``Syntax``

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

> TcpServer ``Syntax``

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
