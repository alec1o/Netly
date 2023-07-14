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

##### Versions
>  <sub>Notable changes</sub>
  
| <sub>v1 (old)</sub>                     | <sub>v2 (stable)</sub> | <sub>v3 (in dev)</sub> | <sub>v4 (nonexistent)</sub> |
| ---                          | ---          | ---              | ---              |
|<sub>TCP (client/server)</sub>| <sub> TCP/IP [Message Framing](https://web.archive.org/web/20230219220947/https://blog.stephencleary.com/2009/04/message-framing.html)</sub> | <sub>TLS/SSL (client/server)</sub> | <sub>Websocket (client/server)</sub> |
| <sub>UDP</sub> | <sub>TCP/UDP performance improvement</sub> | <sub>Include docs/sample (SSL/TLS)</sub> |  <sub>Include docs/sample (Websocket)</sub> | 
|                |                                            | <sub>``Message Framing`` memory and performance improve</sub> |                            | 
|                |                                            | <sub>``Message Framing`` new protocol</sub> |                            | 

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
> <sub>Warning `MessageFraming`</sub>
  ```php
MessageFraming: just set this to false if you want to use netly to communicate with another tcp library.
true case: this will improve data security, but both client and server must have the same configuration.
  ```
> <sub>Client</sub>
  ```csharp
  using Netly;
  using Netly.Core;

  /* ================ Instances ================ */

  var client = new TcpClient(messageFraming: true); 
  var host = new Host("127.0.0.1", 3000); 

  /* ================ Triggers ================= */

  client.Open(host); // open connection

  client.ToData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9}); // send raw data

  client.ToEvent("name", new byte[] { 1, 2, 3, 4, 5, 6}); // send event

  client.Close(); // close connection

  /* ================ Callbacks ================ */

  client.OnOpen(() =>
  {
      // connection opened
  });

  client.OnClose(() =>
  {
      // connection closed
  });

  client.OnError((exception) =>
  {   
      // error on open connection
  });

  client.OnData((data) => 
  {
      // buffer/data received
  });

  client.OnEvent((name, data) =>
  {
      // event received: {name: event name} {data: buffer/data received} 
  });

  client.OnModify((socket) =>
  {
      // modify socket instance
  });
  ```
> <sub>Server</sub>
  ```csharp
  using Netly;
  using Netly.Core;

  /* ================ Instances ================ */

  var server = new TcpServer(messageFraming: true);
  var host = new Host("0.0.0.0", 3000);

  /* ================ Triggers ================= */  

  server.Open(host); // open connection

  server.ToData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9}); // broadcast data

  server.ToEvent("name", new byte[] { 1, 2, 3, 4, 5, 6}); // broadcast event

  server.Close(); // close connection

  /* ================ Callbacks ================ */  

  server.OnOpen(() =>
  {
      // connection opened: server start listen client
  });

  server.OnClose(() =>
  {
      // connection closed: server stop listen client
  });

  server.OnError((exception) =>
  {
      // error on open connection
  });

  server.OnEnter((client) =>
  {
      // client connected: connection accepted
  });

  server.OnExit((client) =>
  {
      // client disconnected: connection closed
  });

  server.OnData((client, data) =>
  {
      // buffer/data received: {client: client instance} {data: buffer/data received} 
  });

  server.OnEvent((client, name, data) =>
  {
      // event received: {client: client instance} {name: event name} {data: buffer received} 
  });

  server.OnModify((socket) =>
  {
      // modify socket instance
  });
  ```
