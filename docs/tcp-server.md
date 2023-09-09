# <return>class</return> TcpServer

###### Tcp server implementation

## Namespace
```cs
using Netly;
```

<br>

## Constructors
- ##### <return>TcpServer</return> TcpServer(<params>bool framing</params>)
    - ``framing`` "enable or disable ``Stream protocol message framing``
        - ``true`` Netly will use its own message framing protocol    
        - ``false`` You will receive raw stream data without framing middleware. For receive raw data use <params>TcpServer.OnData(Action<TcpClient, byte[]> callback)</params>

<br>

## Properties
- ##### <return>bool</return> IsOpened
  <sub>Return ``true`` when the server socket is connected (bind port and listen clients) and ``false`` if not.

</sub>

<br>

- ##### <return>Host</return> Host
  <sub>``Host class`` is endpoint container, and contain (ip address, port, ip type), it's endpoint metadata.</sub>

<br>

- ##### <return>bool</return> Framing
  <sub>Return ``true`` when the instance is using ``Netly MessageFraming protocol`` and ``false`` when the instance is not using ``MessageFraming protocol``</sub>

<br>

- ##### <return>bool</return> IsEncrypted 
  <sub>Return ``true`` when the instance is using ``TLS/SSL`` and ``false`` when isn't using this.</sub>

<br>

- ##### <return>TcpClient[]</return> Clients
  <sub>Return array of all connected client (``TcpClient``)</sub>

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


- ##### <return>void</return> OnEnter(<params>Action&lt;TcpClient&gt; callback</params>)
  <sub>This event is responsible for receive client when connected</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>

- ##### <return>void</return> OnExit(<params>Action&lt;TcpClient&gt; callback</params>)
  <sub>This event is responsible for detect when a client be disconnected</sub></sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>  
  - <return>Wrapper</return> 
    ```cs
    TcpServer.OnEnter((client) =>
    {
        client.OnClose(() =>
        {
            Console.WriteLine(client.UUID);
        });
    });  
    ```

<br>

- ##### <return>void</return> OnData(<params>Action&lt;TcpClient, byte[]&gt; callback</params>)
  <sub>Event responsible for receiving raw data (buffer) from the client</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>
  - <return>Wrapper</return>
  ```cs
  TcpServer.OnEnter((client) =>
  {
      client.OnData((buffer) =>
      {
          Console.WriteLine(client.UUID);
      });
  });
  ```


<br>


- ##### <return>void</return> OnEvent(<params>Action&lt;TcpClient, string, byte[]&gt; callback</params>)
  <sub>Event responsible for receiving events (netly-events) from the client.</sub>
  - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>
  - <return>Wrapper</return>
  ```cs
  TcpServer.OnEnter((client) =>
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