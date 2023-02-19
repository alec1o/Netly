<h1 align="center"><a href="https://github.com/alec1o/netly">Netly</a></h1>

<h6 align="end">
  License <a href="LICENSE.md">SEE HERE (MIT)</a><br>
  Support <a href="mailto://support@kezero.com">CONTACT HERE</a>
</h6>

<h6 align="center">
  powered by <a href="https://github.com/alec1o">ALEC1O</a>
</h6>

<h6 align="center">
  <img align="center" src="content/logo/netly-logo-3.png" width="128px">
<h6>

## About
> Netly is a open source socket library for c# (C-Sharp). It facilitates the use of socket (UDP and TCP, Client and Server) with which it is compatible (Android, iOS, macOS, Linux, Windows, ...) as long as it is compiled with its destination.

<br>

## Docs
> ### See the documentation [here!](http://netly.docs.kezero.com)

<br>

## Versions
| [v1](https://github.com/alec1o/Netly/tree/1.x)                     | [v2](https://github.com/alec1o/Netly/tree/2.x) (current) | [v3](https://github.com/alec1o/Netly/tree/3.x) |
| ---                        | ---    | ---    |
| <h5>TCP client/server</h5> | TCP/IP [Message Framing](https://web.archive.org/web/20230219220947/https://blog.stephencleary.com/2009/04/message-framing.html) | SSL client/server  |
| <h5>UDP client/server</h5> |        |        |

<br>

## Dependency
- ##### [Byter](https://github.com/alec1o/byter)

<br>

## Build
- ### Build dependencies
  ###### [Git](http://git-scm.com/)  
  ###### [MONO](http://mono-project.com) or [.NET](http://dot.net)
  
- ### Build step-by-step 
```rb
# 1. clone repository 
$ git clone "https://github.com/alec1o/netly.git"

# 2. open source directory 
$ cd netly/

# 3. download dependency module
$ git submodule update --init

# 4. optional, update module
$ git submodule update --remote

# 5. restore dotnet project
$ dotnet restore

# 6. build dotnet project
$ dotnet build
```

<br>

## List of tested platforms
- ###### [.NET](https://dotnet.microsoft.com) (SDK)
- ###### [Mono](https://mono-project.com) (SDK)
- ###### [Unity](https://unity.com) (Engine)

<br>
  
## Demo
- ### Client
  _Instance_
  ```csharp
  using Netly;
  using Netly.Core;

  // Example udp client instance
  var client = new UdpClient();

  // Example tcp client instance
  var client = new TcpClient();

  // Example host instance
  var host = new Host("127.0.0.1", 3000);    
  ```
  _Usage_
  ```csharp
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

  // open connection
  client.Open(host);

  // send data
  client.ToData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9});

  // send event
  client.ToEvent("name", new byte[] { 1, 2, 3, 4, 5, 6});

  // close connection
  client.Close();
  ```
- ### Server
  _Instance_
  ```csharp
  using Netly;
  using Netly.Core;

  // Example tcp server instance
  var server = new TcpServer();

  // Example udp server instance
  var server = new UdpServer();

  // Example host instance
  var host = new Host("0.0.0.0", 3000);
  ```
  _Usage_
  ```csharp
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

  // open connection
  server.Open(host);

  // broadcast data to clients
  server.ToData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9});

  // broadcast event to clients
  server.ToEvent("name", new byte[] { 1, 2, 3, 4, 5, 6});

  // close connection
  server.Close();
  ```
<br>

## Currently missing feature
> Below are some missing features that are planned to be added in later versions.

- [ ] [SslClient and SslServer] Tcp protocol with SSL/TLS encryption.
