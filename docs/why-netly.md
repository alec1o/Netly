# Why Netly?
There are several reasons why I [@alec1o](https://alecio.kezero.com) recommend netly. see below!

## What is Netly?
Netly is a flexible and expandable socket library, written in the C# language (c-sharp) and is constantly evolving and getting new features.

## Free and open-source
Netly is free, free, and open source under the MIT license (permissive) and its code is available on [github, see here](https://github.com/alec1o/Netly)

## Multi protocols
Netly contains an ecosystem that is constantly evolving and it supports multiple protocols that are:
- [TCP](#/)
- [UDP](#/)
- [SSL/TLS](#/)
- [NETLY Message Framing - Fast message framing protocol for stream protocols (TCP/SSL/TLS)](#/)

## Robust syntax

- TCP Client
    ```cs
    using Netly;
    using Netly.Core;

    var client = new TcpClient(framing: true);

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
    
    client.OnModify((Socket socket) =>
    {
        
    });

    client.OnEvent((string name, byte[] data) =>
    {

    });
    
    client.Open(new Host("127.0.0.1", 8080));
    ```
