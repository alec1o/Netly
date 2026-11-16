---
sidebar_position: 1
---

# Chat

Here is an example of a simple chat application using the Netly TCP library in a .NET console application.

### Example Code

```cs --title="ChatClient"
// Byter: allow serialize and deserialize primitive bytes and provide
// extension methods e.g. byte[].GetString() or string.GetBytes().
using Byter;
using Netly;
using System;


// create TCP client instance.
// framing: receive data in chunks and release it as completed
// disable framing if are you using your own framing logic.
var client = new TCP.Client(isFraming: true);


// callback called when client open connection successful
client.On.Open(() =>
{
    Console.WriteLine("Connected to server");

    client.To.Data("Hello World!"); // send raw data
    client.To.Event("welcome", "Hello Server!"); // send event
});

// callback called when client close connection
client.On.Close(() =>
{
    Console.WriteLine("Disconnected from server");
});

// callback called can't open connection, exception contain de cause.
client.On.Error((exception) =>
{
    Console.WriteLine("Error: " + exception.Message);
});

// callback called when client receive raw data.
// you can handle this data on your own.
client.On.Data((bytes) =>
{
    Console.WriteLine("Received raw message: " + bytes.GetString());
});

// callback called when client receive event.
// event is a string, and data is a byte array.
// event only work on netly server/client.
// is same that raw data but is internal implemented
// to make easy and fast data manipulation.
// event name contain name of event, event data contain bytes of event data.
client.On.Event((string name, byte[] bytes) =>
{
    if (name == "chat")
        Console.WriteLine($"CHAT: {bytes.GetString()}");
    else
        Console.WriteLine($"Unknown event: {name}");
});

client.To.Open(new Host("127.0.0.1", 8080));

while (true)
{
    Console.WriteLine("Enter message: ");
    string message = Console.ReadLine();
    client.To.Event("CHAT", message);
}

client.To.Close();
```

<br/>

:::info
Instance type of TCP.Client is same on server and client side, it is changed internal to work as client-side and server-side, it mean the unique differences is TCP.Client on server-side can't allow open connection from `TCP.Client.To.Open(<host>)` but other things is same.
:::

<br/>

:::warning
Note framing must be enabled or disable on client and server, if not parts who have it enabled will close connection because can't fragment the buffer on parts that have it disabled.
:::

:::tip
If framing is enabled you can e.g use `TCP.Client.To.Event|Data` to send e.g 10MB server will receive this 10MB on single callback instead of chunked in short bytes. To prevent receive large size of bytes you can configure max of package allowed from `Netly.Environment.MessageFraming.MaxSize` in bytes, by default is `8MB or 8,388,608 bytes` if another part send more than allowed size connection will be closed.
:::

<br/>

```csharp --title="ChatServer"
using Netly;
using Byter;
using System;
using System.Collections.Generic;

var server = new TCP.Server(isFraming: true);

server.On.Open(() =>
{
    Console.WriteLine($"Server started at: {server.Host}");
});

server.On.Close(() =>
{
    Console.WriteLine($"Server stopped at: {server.Host}");
});

server.On.Error((exception) =>
{
    Console.WriteLine($"Error: {exception}");
});

server.On.Accept((client) =>
{
    // scope reserved to each client
    // each client have on scope like this.

    // it mean each client will have own instance of eventData and rawData
    List<(string message, byte[] data)> eventData = new();
    List<byte[]> rawData = new();


    client.On.Open(() =>
    {
        // client connected
    });

    client.On.Close(() =>
    {
        // client closed;
        Console.WriteLine($"Client {client.Host} disconnected");

        // clean all data received by this client
        rawData.Clear();
        eventData.Clear();
    });

    client.On.Data((bytes) =>
    {
        Console.WriteLine($"Client data ({client.Host}): {bytes.GetString()}");
        server.To.DataBroadcast(bytes); // broadcast data

        // save history of data
        rawData.Add(bytes);
    });

    client.On.Event((name, bytes) =>
    {
        Console.WriteLine($"Client event ({name}): ({client.Host}): {bytes.GetString()}");
        server.To.EventBroadcast(name, bytes); // broadcast event

        // save history of events
        eventData.Add((name, bytes));
    });
});

server.To.Open(new Host("127.0.0.1", 8080));

Console.WriteLine("Server is running. Press any key to stop.");
Console.ReadKey();
server.To.Close();

```

### Example Demo (\*.GIF)

![TCP Chat Demo](NetlyTcpChat.gif)
