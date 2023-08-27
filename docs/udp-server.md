# <return>class</return> UdpServer

###### Udp server implementation

## Namespace
```cs
using Netly;
```

<br>

## Constructors
- ##### <return>UdpServer</return> UdpServer(<params>bool useConnection</params>, <params>int? timeout</params>)
    - ``useConnection`` "enable or disable udp connection (using ping)
    - ``timeout`` is connection timeout (only when ``useConnection`` is enabled

<br>

## Properties
- ##### <return>bool</return> IsOpened
  <sub>Return ``true`` when connection be connected (when server listen and receive client buffer), and ``false`` when connection isn't connected.</sub>

<br>

- ##### <return>Host</return> Host
  <sub>``Host class`` is endpoint container, and contain (ip address, port, ip type), it's endpoint metadata.</sub>

<br>

- ##### <return>int</return> Timeout
  <sub>It's timeout value and is used for determine connection timeout, timeout is ``milliseconds`` and only be used when ``UseConnection`` is enabled (``true``)</sub>

<br>

- ##### <return>bool</return> UseConnection
  <sub>It's determine if netly can use connection abstraction, if enabled (``true``) netly will send frequently ping message, and ``timeout`` for detect connection is alive.</sub>

<br>

- ##### <return>UdpClient[]</return> Clients
  <sub>Return array of all connected client (``UdpClient``).<br>``Warning``: client will just disconnected when ``UseConnection`` is enabled (``true``).</sub>

<br>

## Methods
> Triggers

- ##### <return>void</return> Open(<params>Host host</params>)
  <sub>Open connection (bind and listen client), if connection not be open will call and expose exception on ``OnError`` callback.</sub>
  - <sub>``Host`` Netly host instance (endpoint metadata)<sub/>

<br>

- ##### <return>void</return> Close()
  <sub>Close connection (stop receive client buffer), if you need close connection use this method.</sub>

<br>

- ##### <return>void</return> ToData(<params>byte[] buffer</params>) <br> <return>void</return> ToData(<params>string buffer</params>)
  <sub>Broadcast raw buffer to all connected clients from <params>Clients</params> array, ``buffer`` is ``string`` or ``byte[]`` (bytes).</sub>

<br>

- ##### <return>void</return> ToEvent(<params>string name</params>, <params>byte[] buffer</params>) <br> <return>void</return> ToEvent(<params>string name</params>, <params>string buffer</params>)
  <sub>Broadcast event (netly-event) to all connected clients from <params>Clients</params> array, ``name`` is event identifier, ``buffer`` is event buffer (data), buffer is ``string`` or ``byte[]`` (bytes), if send buffer as string, netly will use ``NE.Default`` as encoding protocol.</sub>

<br>

> Callbacks

- ##### <return>void</return> OnOpen(<params>Action callback</params>)
  <sub>Event responsible for receiving the connection opening information successfully</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>


- ##### <return>void</return> OnError(<params>Action&lt;Exception&gt; callback</params>)
  <sub>Event responsible for receiving an error when opening a connection, the ``Exception`` contains the error information.</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>


- ##### <return>void</return> OnClose(<params>Action callback</params>)
  <sub>Called when server close connection</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>


- ##### <return>void</return> OnEnter(<params>Action&lt;UdpClient&gt; callback</params>)
  <sub>This event is responsible for receive client when connected</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>

- ##### <return>void</return> OnExit(<params>Action&lt;UdpClient&gt; callback</params>)
  <sub>This event is responsible for detect when a client be disconnected</sub></sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>  
  - <return>Wrapper</return> 
    ```cs
    UdpServer.OnEnter((client) =>
    {
        client.OnClose(() =>
        {
            Console.WriteLine(client.UUID);
        });
    });  
    ```

<br>

- ##### <return>void</return> OnData(<params>Action&lt;UdpClient, byte[]&gt; callback</params>)
  <sub>Event responsible for receiving raw data (buffer) from the client</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>
  - <return>Wrapper</return>
  ```cs
  UdpServer.OnEnter((client) =>
  {
      client.OnData((buffer) =>
      {
          Console.WriteLine(client.UUID);
      });
  });
  ```


<br>


- ##### <return>void</return> OnEvent(<params>Action&lt;UdpClient, string, byte[]&gt; callback</params>)
  <sub>Event responsible for receiving events (netly-events) from the client.</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>
  - <return>Wrapper</return>
  ```cs
  UdpServer.OnEnter((client) =>
  {
      client.OnEvent((name, buffer) =>
      {
          Console.WriteLine(client.UUID);
      });
  });
  ```

<br>

- ##### <return>void</return> OnModify(<params>Action&lt;Socket&gt; socket</params>)
  <sub>This event is responsible for executing modifications in ``Socket``, this event is executed in the connection creation step, and you will have access to ``Socket`` that will be used internally</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>

## Example
```cs
using System;
using Netly;
using Netly.Core;

const string ip = "127.0.0.1";
const int port = 3000;
const int myTimeout = 10000; // 10s

Host host = new Host(ip, port);
UdpServer server = new UdpServer(useConnection: true, timeout: myTimeout);

server.OnOpen(() =>
{
    // connection opened.
    Console.WriteLine("Connection opened.");
});

server.OnError((exception) =>
{
    // connection opened.
    Console.WriteLine($"Connection error: {exception}.");  
});

server.OnClose(() =>
{
    // connection closed.
    Console.WriteLine("Connection closed.");
});

server.OnEnter((client) =>
{
    // client connected.
    Console.WriteLine($"Client {client.UUID} connected, host: {client.Host}");    
    
    // example. UdpServer.OnClose Wrapper
    client.OnClose(() =>
    {
        Console.WriteLine($"Client {client.UUID} diconneted, host: {client.Host}");
    };
    
    // example. Event counter (Easy behaviour replication, because unique scope);
    
    // this example is complex for doing using UdpServer.OnEvent because ``counterValue`` scope
    // if you need replicate on UdpServer.OnEvent, you can use key/value class or similar, key is 
    // client.UUID and value is counter, and you can do loop on all key for increment or get counter. 
    int counterValue = 0;    
    client.OnEvent((name, buffer) =>
    {
        counterValue ++;        
        client.ToEvent("event counter", counterValue.ToString());        
    });
});

server.OnExit((client) =>
{
    // client disconneted.
    Console.WriteLine($"Client {client.UUID} diconneted, host: {client.Host}");
});

server.OnData((client, buffer) =>
{
    // server receive raw data from client.
    Console.WriteLine($"Client raw data: {client.Host} -> {NE.GetString(buffer)}");
    
    // echo
    client.ToData(buffer);
});
   
server.OnEvent((client, name, buffer) =>
{
    Console.WriteLine($"Client event ({name}): {client.Host} -> {NE.GetString(buffer)}");

    // echo
    client.ToEvent(name, buffer);    
    
    
    // detect close message
    if (name == "goodbye")
    {
        Console.WriteLine($"connection {client.Host}, talk goodbye.");
        client.Close();
    }    
});

// Open connection
server.Open(host);

// Block main thread for not end program (console application)
Console.ReadLine();
```
