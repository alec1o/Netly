# <return>class</return> TcpClient

###### Tcp client implementation

## Namespace
```cs
using Netly;
```

<br>

## Constructors
- ##### <return>TcpClient</return> TcpClient(<params>bool framing</params>)
    - ``framing`` Enable or disable ``Netly.Core.MessageFraming``.
      - ``true`` Netly will use its own message framing protocol.
      - ``false`` You will receive raw stream data without framing middleware (for receive raw data use ``TcpClient.OnData(Action<byte[]> callback)``)

<br>


## Properties
- ##### <return>bool</return> IsOpened
  <sub>Return ``true`` when connection be connected, and ``false`` when connection isn't connected.</sub>

<br>

- ##### <return>Host</return> Host
  <sub>``Host class`` is endpoint container, and contain (ip address, port, ip type), it's endpoint metadata.</sub>

<br>

- ##### <return>bool</return> Framing
  <sub>Return true when the instance is using ``MessageFraming protocol`` connection and false when the connection is not using ``MessageFraming protocol``</sub>

<br>

- ##### <return>bool</return> IsEncrypted
  <sub>Return true when the instance is using ``TLS/SSL`` and false when isn't using this.</sub>

<br>

- ##### <return>string</return> UUID
  <sub>``UUID`` or (Unique User Identifier) is a unique string value that link a user, only ``server-side`` client.</sub>

<br>

## Methods
> Triggers

- ##### <return>void</return> Open(<params>Host host</params>)
  <sub>Open connection, if connection not be open will call and expose exception on ``OnError`` callback.</sub>
    - <sub>``Host`` Netly host instance (endpoint metadata)<sub/>

<br>

- ##### <return>void</return> UseEncryption(<params>bool enableEncryption</params>, <params>Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> onValidation = null</params>)
  <sub>Enable TLS/SSL connection on ``client side``. (Must used before ``TcpClient.Open(Host host)``)</sub>
    - <sub>``enableEncryption`` Set <params>true</params> for enable ``TLS/SSL`` and set <params>false</params> don't enable ``SSL/TLS``<sub/>
    - <sub>``onValidation`` Is a ``Func`` that will executed for validate server certificate. Default behaviour return ``true`` without validate any argument. See examples on ``TLS/SSL`` session.
<br>

- ##### <return>void</return> Close()
  <sub>Close connection, if you need close connection use this method.</sub>

<br>

- ##### <return>void</return> ToData(<params>byte[] buffer</params>) <br> <return>void</return> ToData(<params>string buffer</params>)
  <sub>Send raw buffer to server, ``buffer`` is ``string`` or ``byte[]`` (bytes).</sub>

<br>

- ##### <return>void</return> ToEvent(<params>string name</params>, <params>byte[] buffer</params>) <br> <return>void</return> ToEvent(<params>string name</params>, <params>string buffer</params>)
  <sub>Send event (netly-event) to server, ``name`` is event identifier, ``buffer`` is event buffer (data), buffer is ``string`` or ``byte[]`` (bytes), if send buffer as string, netly will use ``NE.Default`` as encoding protocol.</sub>

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
  <sub>Event responsible for receiving information about closing or terminating the connection with the server.</sub>
    - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>


- ##### <return>void</return> OnData(<params>Action&lt;byte[]&gt; callback</params>)
  <sub>Event responsible for receiving raw data (buffer) from the server</sub>
    - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>


- ##### <return>void</return> OnEvent(<params>Action&lt;string, byte[]&gt; callback</params>)
  <sub>Event responsible for receiving events (netly-events) from the server</sub>
    - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>


- ##### <return>void</return> OnModify(<params>Action&lt;Socket&gt; socket</params>)
  <sub>This event 'is responsible for executing modifications in ``Socket``, this event is executed in the connection creation step, and you will have access to ``Socket`` that will be used internally</sub>
    - <sub>``callback`` is the "function" responsible for handling the received event.<sub/>

<br>

## Example (dotnet >= 6.0)
```cs
using System;
using Netly;
using Netly.Core;

int pingCounter = 0;
Host host = new Host("127.0.0.1", 8080);
TcpClient client = new TcpClient(framing: true);

// Enable TLS/SSL
// onValidation is optional, default behaviour return true without check any data.
client.UseConnection(enableConnection: true, onValidation: (sender, cert, chain, policyErrors) =>
{
#if DEBUG
    
    /*
        Default behaviour, Ignore certificate validation.
    */
    
    return true;
    
#else 
    /*
        Custom validation callback;
    */
    
    if (sslPolicyErrors == SslPolicyErrors.None) return true;
    
    // ignore chain errors as where self signed
    if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors) return true;
    
    // Invalid SSL connection
    return false;
#endif
});

client.OnOpen(() =>
{
    // connection opened
    client.ToEvent("welcome", "hello server!");
    Console.WriteLine("Client connected!");
});

client.OnError((e) =>
{
    // called when connection not opened or server isn't using framing protocol (if framing is enabled)
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