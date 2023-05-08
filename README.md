<h1 align="center"><a href="https://github.com/alec1o/netly">Netly</a></h1>

<h6 align="end">
  <a href="https://netly.docs.kezero.com">DOCUMENTATION</a>
</h6>

<h6 align="center">
  powered by <a href="https://github.com/alec1o">ALEC1O</a>
</h6>

<h6 align="center">
  <img align="center" src="content/logo/netly-logo-3.png" width="128px">
<h6>

### About
> Netly is a flexible socket library built on c-sharp. It is compatible with (Android, iOS, Linux, Windows...)

<br>

### Install
###### Official publisher
| Nuget | Unity Asset Store |
| ---   | ---               |
| Install on [Nuget](https://www.nuget.org/packages/Netly)| Install on [Asset Store ](https://assetstore.unity.com/packages/tools/network/225473)|

<br>

### Versions
###### Notable changes
| <h5>v1 (old)</h5>                     | <h5>v2 (current)</h5> | <h5>v3 (nonexistent)</h5> | <h5>v4 (nonexistent)</h5> |
| ---                          | ---          | ---              | ---              |
|<h6>TCP (client/server)</h6>| <h6> TCP/IP [Message Framing](https://web.archive.org/web/20230219220947/https://blog.stephencleary.com/2009/04/message-framing.html)</h64> | <h6>TLS/SSL (client/server)</h6> | <h6>Websocket (client/server)</h6> |
| <h6 valign="center">UDP</h6> | <h6 valign="center">TCP/UDP performance improvement</h6> | <h6>Include docs/sample (SSL/TLS)</h6> |  <h6>Include docs/sample (Websocket)</h6> | 

<br>

### Feature
> Below are some missing features that are planned to be added in later versions.

- [ ] SSL/TLS (client/server) 3.x (2023)
- [ ] Websocket (client/server) 4.x (2024)

<br>

### Dependency
- ###### [Byter](https://github.com/alec1o/byter)

### Build
- #### Build dependencies
  ###### [Git](http://git-scm.com/)  
  ###### [MONO](http://mono-project.com) or [.NET](http://dot.net)
  
- #### Build step-by-step 
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

### List of tested platforms
- ###### [.NET](https://dotnet.microsoft.com) (SDK)
- ###### [Mono](https://mono-project.com) (SDK)
- ###### [Unity](https://unity.com) (Engine)

<br>
  
### Demo
- #### Client
  ```csharp
  using Netly;
  using Netly.Core;
  
  /* ================ Instances ================ */
  
  var client = new TcpClient(); 
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
- #### Server
  ```csharp
  using Netly;
  using Netly.Core;
  
  /* ================ Instances ================ */
  
  var server = new TcpServer();
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
<br>
