# TcpServer
``TcpServer`` is a ``class`` from the ``netly library`` that facilitates and simplifies the use of a ``tcp`` connection as a server

## Construtors
- Default
    ```cs
    TcpServer server = new TcpServer(framing: true);
    ```
    - Framing
    ```txt
    Framing:
        Enable or disable netly message framing    
    True:
        Netly will use its own message framing protocol    
    False:
        you will receive raw stream data without framing middleware.
        For receive raw data use "TcpServer.OnData(Action<TcpClient, byte[]> callback)" or
        TcpServer.OnEnter((TcpClient client) =>
        {
            client.OnData((data) => { /* do something here */ }));
        });  
    ```

## Properties
- IsOpened ``bool`` <br>
    <sub>Return true when the server socket is connected (bind port and listen clients) and false if not.</sub>

- Host ``Netly.Core.Host`` <br>
    <sub>It is an object that helps to care for and share a host's credentials.</sub>

- Framing ``bool`` <br>
    <sub>Return true when the instance is using ``MessageFraming protocol`` connection and false when the connection is not using ``MessageFraming protocol``</sub>

- IsEncrypted ``bool`` <br>
    <sub>Return true when the instance is using ``TLS/SSL`` and false when isn't using this.</sub>

## Methods (Trigger)
- Open ``void(Host host, int backlog)`` <br>
    <sub>Used for open connection with server</sub>
    ```cs
    Host host = new Host("127.0.0.1", 8080);
    TcpServer.Open(host, 10);
    ```
- Close ``void()`` <br>
    <sub>Used for close connection with server</sub>
    ```cs
    TcpServer.Close();
    ```
- ToData ``void(byte[] buffer)`` ``void(string buffer)`` <br>
    <sub>Used for send buffer (data) for all connected client</sub>
    ```cs
    string stringBuffer = "Broadcast: Hello world!";
    TcpServer.ToData(stringBuffer);

    byte[] bytesBuffer = NE.GetBytes("Broadcast: Hello world!");
    TcpServer.ToData(bytesBuffer);
    ```
- ToEvent ``void(string name, byte[] buffer)`` ``void(string name, string buffer)`` <br>
    <sub>Used for send event for all connected client using NETLY_EVENT_PROTOCOL</sub>
    ```cs
    string stringBuffer = "Hello world!";
    TcpServer.ToEvent("broadcast-ping-from-string", stringBuffer);

    byte[] bytesBuffer = NE.GetBytes("Hello world!");
    TcpServer.ToEvent("broadcast-ping-from-bytes", bytesBuffer);
    ```
- UseEncryption ``void(byte[] pfxCertificate, string pfxPassword, SslProtocols encryptionProtocol)`` <br>
    <sub>Used for enable TLS/SSL from server side</sub>
    ```cs
    // Warning: See about generate pfx on SSL/TLS page now we will see about startup this!
    // Warning: Convert pfx file for bytes only using ASCII or UTF8
    byte[] pfx = MyClass.GetPfxAsBytes();
    string pfxPassword = MyClass.GetPfxPasswordFromEnvVars() ?? "my-secret-pfx-password"

    // Enable SSL/TLS
    TcpServer.UseEncryption(pfx, pfxPassword, SslProtocols.Tls12);
    ```



## Methods (Callback)
- OnOpen ``void(Action callback)`` <br>
    <sub>Invoke ``callback`` when connection opened</sub>
    ```cs
    TcpServer.OnOpen(() =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnClose ``void(Action callback)`` <br>
    <sub>Invoke ``callback`` when connection closed</sub>
    ```cs
    TcpServer.OnClose(() =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnError ``void(Action<Exception> callback)`` <br>
    <sub>Invoke ``callback``when the instance receives a internal error on opening connection or when receive invalid buffer (when MessageFraming are enabled)</sub>
    ```cs
    TcpServer.OnError((Exception e) =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnData ``void(Action<TcpClient, byte[]> callback)`` <br>
    <sub>Invoke ``callback``when receive a buffer (data)</sub>
    ```cs
    TcpServer.OnData((TcpClient client, byte[] data) =>
    {
        client.ToData(data); // echo: re-send received message
    });
    ```
- OnEvent ``void(Action<TcpClient, string, byte[]> callback)`` <br>
    <sub>Invoke ``callback`` when receive a event (NETLY_EVENT_PROTOCOL)</sub>
    ```cs
    TcpServer.OnEvent((TcpClient client, string name, byte[] data) =>
    {
        if(name == "hello")
        {
            client.ToEvent("hello-callback",  "😅 Alecio is funny");
        }
        else
        {
            client.ToEvent("non-hello", "😅 Netly is fast");
        }
    });
    ```
- OnModify ``void(Action<Socket> callback)`` <br>
    <sub>Invoke ``callback`` when mouting socket instance for custom socket config (before open connection)</sub>
    ```cs
    TcpServer.OnModify((Socket socket) =>
    {
        socket.NoDelay = true;
        print("😅 Alecio is funny");
    });
    ```
- OnEnter ``void(TcpClient client)`` <br>
    <sub>called when client connected on server</sub>
    ```cs
    TcpServer.OnEnter((TcpClient client) =>
    {
        print($"client connected from {client.Host}");
        
        MyClass.AddClient(client, client.UUID);

        // alternative of TcpServer.OnData((client, data) => { ... })
        client.OnData((byte[] data) =>
        {
            client.OnData(data); // resend data for client (echo)
        });

        // alternative of TcpServer.OnEvent((client, name, data) => { ... })
        client.OnEvent((string name, byte[] data) =>
        {            
            client.ToEvent(name, data); // resend event for client (echo)
        });

        // alternative of TcpServer.OnExit((client) => { ... })
        client.OnClose(() =>
        {
            MyClass.RemoveClient(client.UUID);
        });
    })
    ```
- OnExit ``void(TcpClient client)`` <br>
    <sub>called when client is disconnected on server</sub>
    ```cs
    TcpServer.OnExit((TcpClient client) =>
    {
        print($"client disconnected from {client.Host}");
        MyClass.RemoveClient(client.UUID);
    })
    ```

## Example (dotnet >= 6.0)
```cs
using System;
using Netly;
using Netly.Core;


TcpServer server = new TcpServer(framing: true);

server.OnOpen(() =>
{
    Console.WriteLine("Server connected!");
});

server.OnError((e) =>
{
    // called when connection not opened
    Console.WriteLine($"Connection error: {e}");
});

server.OnClose(() =>
{
    Console.WriteLine("Server disconnected!");        
});

server.OnEvent((client, name, data) =>
{
    client.ToEvent(name, data);
    Console.WriteLine($"Client event ({name}) -> {NE.GetString(data)}");
});

// open connection
const int backlog = 10;
server.Open(host, backlog);

// block main thread
bool running = false;
white(running)
{
    Console.WriteLine("Netly ``TcpServer``\n\tq: Quit\n\tc: Close connection\n\tr: Reconnect");
    string input = Console.ReadLine() ?? "";
   
    switch(input)
    {
        case "q":
            server.Close();
            running = false;
            break;            
        case "c":
            server.Close();
            break;
        case "r":
            server.Open(host, backlog);
            break;        
    }
}

Console.WriteLine("Goodbye!!!");

```