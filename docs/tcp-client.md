# TcpClient
``TcpClient`` is a ``class`` from the ``netly library`` that facilitates and simplifies the use of a ``tcp`` connection as a client

## Construtors
- Default
    ```cs
    TcpClient client = new TcpClient(framing: true);
    ```
    - Framing
    ```txt
    Framing:
        Enable or disable netly message framing    
    True:
        Netly will use its own message framing protocol    
    False:
        you will receive raw stream data without framing middleware.
        For receive raw data use "TcpClient.OnData(Action<byte[]> callback)"   
    ```

## Properties
- OnOpen ``bool`` <br>
    <sub>Return true when the socket is connected and returns false when the socket is disconnected.</sub>

- Host ``Netly.Core.Host`` <br>
    <sub>It is an object that helps to care for and share a host's credentials.</sub>

- Framing ``bool`` <br>
    <sub>Return true when the instance is using ``MessageFraming protocol`` connection and false when the connection is not using ``MessageFraming protocol``</sub>

- IsEncrypted ``bool`` <br>
    <sub>Return true when the instance is using ``TLS/SSL`` and false when isn't using this.</sub>

## Methods (Trigger)
- Open ``void(Host host)`` <br>
    <sub>Used for open connection with server</sub>
    ```cs
    Host host = new Host("127.0.0.1", 8080);
    TcpClient.Open(host);
    ```
- Close ``void()`` <br>
    <sub>Used for close connection with server</sub>
    ```cs
    TcpClient.Close();
    ```
- ToData ``void(byte[] buffer)`` ``void(string buffer)`` <br>
    <sub>Used for send raw buffer for server</sub>
    ```cs
    string stringBuffer = "Hello world!";
    TcpClient.ToData(stringBuffer);

    byte[] bytesBuffer = NE.GetBytes("Hello world!");
    TcpClient.ToData(bytesBuffer);
    ```
- ToEvent ``void(string name, byte[] buffer)`` ``void(string name, string buffer)`` <br>
    <sub>Used for send event (NETLY_EVENT_PROTOCOL) for server</sub>
    ```cs
    string stringBuffer = "Hello world!";
    TcpClient.ToEvent("ping-its-string", stringBuffer);

    byte[] bytesBuffer = NE.GetBytes("Hello world!");
    TcpClient.ToEvent("ping-its-bytes", bytesBuffer);
    ```
- UseEncryption ``void(bool enableEncryption, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> onValidation = null)`` <br>
    <sub>Used for enable TLS/SSL from client side</sub>
    ```cs
    // Default way for enable and verify tls/ssl from client side
    TcpClient.UseEncryption(true);

    // Example of custom way for enable and verify tls/ssl from client side
    TcpClient.UseEncryption(true, (sender, cert, chain, erros) => {
        if (cert == null) return false;
        if (chain.ChainElements.Count != 3) return false;
        if (errors != SslPolicyErrors.None) return false;
        return true;
    })
    ```



## Methods (Callback)
- OnOpen ``void(Action callback)`` <br>
    <sub>Invoke ``callback`` when connection opened</sub>
    ```cs
    TcpClient.OnOpen(() =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnClose ``void(Action callback)`` <br>
    <sub>Invoke ``callback`` when connection closed</sub>
    ```cs
    TcpClient.OnClose(() =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnError ``void(Action<Exception> callback)`` <br>
    <sub>Invoke ``callback``when the instance receives a internal error on opening connection or when receive invalid buffer (when MessageFraming are enabled)</sub>
    ```cs
    TcpClient.OnError((Exception e) =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnData ``void(Action<byte[]> callback)`` <br>
    <sub>Invoke ``callback``when receive a buffer (data)</sub>
    ```cs
    TcpClient.OnData((byte[] data) =>
    {
        print("😅 Alecio is funny");
    });
    ```
- OnEvent ``void(Action<string, byte[]> callback)`` <br>
    <sub>Invoke ``callback`` when receive a event (NETLY_EVENT_PROTOCOL)</sub>
    ```cs
    TcpClient.OnEvent((string name, byte[] data) =>
    {
        if(name == "hello")
        {
            print("😅 Alecio is funny");
        }
        else
        {
            print("😅 Netly is fast");
        }
    });
    ```
- OnModify ``void(Action<Socket> callback)`` <br>
    <sub>Invoke ``callback`` when mouting socket instance for custom socket config (before open connection)</sub>
    ```cs
    TcpClient.OnModify((Socket socket) =>
    {
        socket.NoDelay = true;
        print("😅 Alecio is funny");
    });
    ```

## Example (dotnet >= 6.0)
```cs
using System;
using Netly;
using Netly.Core;

int pingCounter = 0;
Host host = new Host("127.0.0.1", 8080);
TcpClient client = new TcpClient(framing: true);

client.OnOpen(() =>
{
    client.ToEvent("welcome", "hello server!");
    Console.WriteLine("Client connected!");
});

client.OnError((e) =>
{
    // called when connection not opened or server isnt using framing protocol
    Console.WriteLine($"Connection error: {e}");
});

client.OnClose(() =>
{
    Console.WriteLine("Client disconnected!");        
});

client.OnEvent((name, data) =>
{
    name = name.ToLower().Trim(); // convert string to lower case

    if(name == "ping")
    {
        pingCounter++;
        client.ToEvent("pong", $"ping counter: {pingCounter}");
    }
    else if(name  == "q" || name == "quit" || name == "done")
    {
        client.Close();
    }

    Console.WriteLine($"Client event ({name}) -> {NE.GetString(data)}");
});

// open connection
client.Open(host);

// block main thread
bool running = false;
white(running)
{
    Console.WriteLine("Netly ``TcpClient``\n\tq: Quit\n\tc: Close connection\n\tr: Reconnect");
    string input = Console.ReadLine() ?? "";
   
    switch(input)
    {
        case "q":
            client.Close();
            running = false;
            break;            
        case "c":
            client.Close();
            break;
        case "r":
            client.Open(host);
            break;        
    }
}

Console.WriteLine("Goodbye!!!");

```