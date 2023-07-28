<h1 align="center"><a href="https://github.com/alec1o/netly">Netly</a></h1>

<h6 align="center"><sub>
  powered by <a href="https://github.com/alec1o">ALEC1O</a><sub/>
</h6>

<h6 align="center">
  <img align="center" src="content/logo/netly-logo-3.png" width="128px">
<h6>

##### About
> <sub>Netly is a flexible socket library built on c-sharp. It is compatible with (Android, iOS, Linux, Windows...)</sub>

<br>
  
##### Documentation 
> <sub>Netly docs ([HERE](https://netly.docs.kezero.com))</sub>

<br>

##### Install
>  <sub>Official publisher</sub>
  
  | <sub>Nuget</sub> | <sub>Unity Asset Store</sub> |
| ---   | ---               |
  | <sub>Install on [Nuget](https://www.nuget.org/packages/Netly)</sub>| <sub>Install on [Asset Store ](https://assetstore.unity.com/packages/tools/network/225473)</sub>|

<br>

##### Sponsor and Supporter
> <sub>Well, this project is open source and only development can be supported by suggestions for improvements, bug reports or the like. (for those who want to financially support this resource is not available at this time)</sub>

<br>

##### Versions
>  <sub>Notable changes</sub>
  
| <sub>v1 (old)</sub>                     | <sub>v2 (stable)</sub> | <sub>v3 (in dev)</sub> | <sub>v4 (nonexistent)</sub> |
| ---                          | ---          | ---              | ---              |
|<sub>TCP (client/server)</sub>| <sub> TCP/IP [Message Framing](https://web.archive.org/web/20230219220947/https://blog.stephencleary.com/2009/04/message-framing.html)</sub> | <sub>TLS/SSL (client/server)</sub> | <sub>Websocket (client/server)</sub> |
| <sub>UDP</sub> | <sub>TCP/UDP performance improvement</sub> | <sub>Include docs/sample (SSL/TLS)</sub> |  <sub>Include docs/sample (Websocket)</sub> | 
|                |                                            | <sub>``Message Framing`` memory and performance improve</sub> |                            | 
|                |                                            | <sub>``Message Framing`` new protocol</sub> |                            |
|                |                                            | <sub>``UDP`` impl connection with udp (ping/timeout)</sub> |                            | 

<br>

##### List of tested platforms
  - <sub>[.NET](https://dotnet.microsoft.com) (SDK)</sub>
  - <sub>[Mono](https://mono-project.com) (SDK)</sub>
  - <sub>[Unity](https://unity.com) (Engine)</sub>
  
<br>

##### Feature
> <sub>Below are some missing features that are planned to be added in later versions.</sub><br>
  - <sub>SSL/TLS (v3)</sub>
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
  # 1. clone repository 
  $ git clone "https://github.com/alec1o/netly.git"

  # 2. open source directory 
  $ cd netly/

  # 5. restore dotnet project
  $ dotnet restore

  # 6. build dotnet project
  $ dotnet build
  ```

<br>
  
##### Demo
> <sub>TcpClient ``Syntax``</sub>
  ```csharp
  using Netly;
  using Netly.Core;
  
  var client = new TcpClient(framing: true);
  
  // Enable SSL/TLS (onValidate delegate is optional)
  client.UseEncryption(enableEncryption: true, onValidate: null);
  
  client.OnOpen(() => 
  {
  
  });
  
  client.OnClose(() =>
  {
      
  });
  
  client.OnError((Exception exception) =>
  {
  
  });
  
  client.OnData((byte[] data) =>
  {
  
  });
  
  client.OnEvent((string name, byte[] data) =>
  {
  
  });
  
  client.OnModify((Socket socket) =>
  {
      
  });
  
  client.Open(new Host("127.0.0.1", 8080));
  ```

> <sub>TcpServer ``Syntax``</sub>
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
  
  });
  
  server.OnClose(() =>
  {
      
  });
  
  server.OnError((Exception exception) =>
  {
  
  });
  
  server.OnData((TcpClient client, byte[] data) =>
  {
  
  });
  
  server.OnEvent((TcpClient client, string name, byte[] data) =>
  {
  
  });
  
  server.OnEnter((TcpClient client) =>
  {
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
     
  });
  
  server.OnModify((Socket socket) =>
  {
      
  });
  
  server.Open(new Host("127.0.0.1", 8080));
  ```
