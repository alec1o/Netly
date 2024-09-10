---
sidebar_position: 1
---

# UdpClient

###### Udp client implementation

## Namespace
```cs
using Netly;
```

<br/>

## Constructors
- ##### <return>UdpClient</return> UdpClient(<params>bool useConnection</params>, <params>int? timeout</params>)
    - ``useConnection`` "enable or disable udp connection (using ping)
    - ``timeout`` is connection timeout (only when ``useConnection`` is enabled

<br/>

## Properties
- ##### <return>bool</return> IsOpened
  <sub>Return ``true`` when connection be connected, and ``false`` when connection isn't connected.</sub>

<br/>

- ##### <return>Host</return> Host
  <sub>``Host class`` is endpoint container, and contain (ip address, port, ip type), it's endpoint metadata.</sub>

<br/>

- ##### <return>int</return> Timeout
  <sub>It's timeout value and is used for determine connection timeout, timeout is ``milliseconds`` and only be used when ``UseConnection`` is enabled (``true``)</sub>

<br/>

- ##### <return>bool</return> UseConnection
  <sub>It's determine if netly can use connection abstraction, if enabled (``true``) netly will send frequently ping message, and ``timeout`` for detect connection is alive.</sub>

<br/>

- ##### <return>string</return> UUID
  <sub>``UUID`` or (Unique User Identifier) is a unique string value that link a user, only ``server-side`` client.</sub>

<br/>

## Methods
> Triggers

- ##### <return>void</return> Open(<params>Host host</params>)
  <sub>Open connection, if connection not be open will call and expose exception on ``OnError`` callback.</sub>
  - <sub>``Host`` Netly host instance (endpoint metadata)</sub>

<br/>

- ##### <return>void</return> Close()
  <sub>Close connection, if you need close connection use this method.</sub>

<br/>

- ##### <return>void</return> ToData(<params>byte[] buffer</params>) <br/> <return>void</return> ToData(<params>string buffer</params>)
  <sub>Send raw buffer to server, ``buffer`` is ``string`` or ``byte[]`` (bytes).</sub>

<br/>

- ##### <return>void</return> ToEvent(<params>string name</params>, <params>byte[] buffer</params>) <br/> <return>void</return> ToEvent(<params>string name</params>, <params>string buffer</params>)
  <sub>Send event (netly-event) to server, ``name`` is event identifier, ``buffer`` is event buffer (data), buffer is ``string`` or ``byte[]`` (bytes), if send buffer as string, netly will use ``NE.Default`` as encoding protocol.</sub>

<br/>

> Callbacks

- ##### <return>void</return> OnOpen(<params>Action callback</params>)
  <sub>Event responsible for receiving the connection opening information successfully</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.</sub>

<br/>


- ##### <return>void</return> OnError(<params>Action&lt;Exception&gt; callback</params>)
  <sub>Event responsible for receiving an error when opening a connection, the ``Exception`` contains the error information.</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.</sub>

<br/>


- ##### <return>void</return> OnClose(<params>Action callback</params>)
  <sub>Event responsible for receiving information about closing or terminating the connection with the server (UDP only when ``UseConnection`` is active (``true``))</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.</sub>

<br/>


- ##### <return>void</return> OnData(<params>Action&lt;byte[]&gt; callback</params>)
  <sub>Event responsible for receiving raw data (buffer) from the server</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.</sub>

<br/>


- ##### <return>void</return> OnEvent(<params>Action&lt;string, byte[]&gt; callback</params>)
  <sub>Event responsible for receiving events (netly-events) from the server</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.</sub>

<br/>


- ##### <return>void</return> OnModify(<params>Action&lt;Socket&gt; socket</params>)
  <sub>This event 'is responsible for executing modifications in ``Socket``, this event is executed in the connection creation step, and you will have access to ``Socket`` that will be used internally</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.</sub>

<br/>

## Example
```cs
using System;
using Netly;
using Netly.Core;

const string ip = "127.0.0.1";
const int port = 3000;
const int myTimeout = 10000; // 10s

Host host = new Host(ip, port);
UdpClient client = new UdpClient(useConnection: true, timeout: myTimeout);

client.OnOpen(() =>
{
    // connection opened.
    Console.WriteLine("Connection opened.");
    
    // send raw data to the server
    client.ToData(NE.GetBytes("Is raw data", NE.Mode.ASCII));
    
    // send event to the server
    client.ToEvent("hello" NE.GetBytes("Is event data", NE.Mode.ASCII));
});

client.OnError((exception) =>
{
    // connection opened.
    Console.WriteLine($"Connection error: {exception}.");
});

client.OnClose(() =>
{
    // connection closed.
    Console.WriteLine("Connection closed.");
});

client.OnData((buffer) =>
{
    // raw data received
    Console.WriteLine($"Raw data: {NE.GetString(buffer)}");
});
   
client.OnEvent((name, buffer) =>
{
    // detect close message
    if (name == "goodbye")
    {
        // receive goodbye event
        Console.WriteLine($"Server sent goodbye -> {NE.GetString(buffer)}");
        client.Close();
    }
    else
    {
        // event received
        Console.WriteLine($"Event: {name} -> {NE.GetString(buffer)}");
    }        
});

// Open connection
client.Open(host);

// Block main thread for not end program (console application)
Console.ReadLine();
```
