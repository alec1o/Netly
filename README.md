<h1 align="center">Netly</h1>

# Docs: ServerBroadCast/CoreNE

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

## List of tested platforms
- [.NET](https://dotnet.microsoft.com) (SDK)
- [Mono](https://mono-project.com) (SDK)
- [Unity](https://unity.com) (Engine)

<br>
  
## Demo
- TcpClient
  ```csharp
  using Netly;

  var host = new Host("127.0.0.1", 3000);
  var client = new TcpClient();

  client.OnOpen(() =>
  {
      Console.WriteLine($"[OPEN]: {host}");
      client.ToData(NE.GetBytes("hello server"));  // send data to server
      client.ToEvent("ping", NE.GetBytes("ping...")); // send event to server
  });

  client.OnClose(() => Console.WriteLine($"[CLOSE]: {host}"));

  client.OnError((e) => Console.WriteLine($"[ERROR]: {e}"));

  client.OnData((data) => Console.WriteLine($"[DATA]: {NE.GetString(data)}"));

  client.OnEvent((name, data) => Console.WriteLine($"[EVENT] -> {name}: {NE.GetString(data)}"));

  client.Open(host);

  // block MainThread
  Console.ReadLine();
  ```
- TcpServer
  ```csharp
  using Netly;

  var host = new Host("0.0.0.0", 3000);
  var server = new TcpServer();

  server.OnOpen(() => Console.WriteLine($"[OPEN]: {host}"));

  server.OnClose(() => Console.WriteLine($"[CLOSE]: {host}"));

  server.OnError((e) => Console.WriteLine($"[ERROR]: {e}"));

  server.OnEnter((client) => Console.WriteLine($"[ENTER]: {client.Host}"));

  server.OnExit((client) => Console.WriteLine($"[EXIT]: {client.Host}"));

  server.OnData((client, data) =>
  {
      client.ToData(data);
      Console.WriteLine($"[DATA] {client.Host}: {NE.GetString(data)}");
  });

  server.OnEvent((client, name, data) =>
  {
      if (name == "ping") client.ToEvent("pong", NE.GetBytes("pong..."));
      Console.WriteLine($"[EVENT] {client.Host} -> {name}: {NE.GetString(data)}");
  });

  server.Open(host);

  // block MainThread
  Console.ReadLine();
  ```
<br>

## Currently missing feature
> Below are some missing features that are planned to be added in later versions.

- [ ] [SslClient and SslServer] Tcp protocol with SSL/TLS encryption.
- [ ] [UDP client/server] Detect connection (using continuous pings and a no-response timeout to identify client/server connection).
- [ ] [ TCP/UDP/SSL Send data in string without having to convert from string to bytes manually.
