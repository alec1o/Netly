# <return>class</return> Host

###### Netly endpoint metadata

## Namespace
```cs
using Netly.Core;
```

## Constructors
- ##### <return>Host</return> Host(<params>string ip</params>, <params>int port</params>) <br> <return>Host</return> Host(<params>IPAddress address</params>, <params>int port</params>) <br> <return>Host</return> Host(<params>IPEndPoint ipEndpoint</params>) <br> <return>Host</return> Host(<params>EndPoint endpoint</params>)
    - ``ip`` The IP address of the endpoint in text format (string format).
    - ``address`` The IP address of the endpoint.
    - ``port`` The port number of the endpoint.
    - ``endpoint`` Identifies a network address. This is an ``abstract`` class.
    - ``ipEndpoint`` Represents a network endpoint as an ``IP address`` and a ``port number``.

<br>

## Properties
- ##### <return>Host</return> Default
  <sub>Return default Host instance, the default host is (0.0.0:0).</sub>

<br>

- ##### <return>IPAddress</return> Address
  <sub>Return IPAddress (The IP address of the endpoint).</sub>

<br>

- ##### <return>int</return> Port
  <sub>Return Port (The port number of the endpoint).</sub>

<br>

- ##### <return>AddressFamily</return> AddressFamily
  <sub>Return AddressFamily (Specifies the addressing scheme that an instance of the Socket class can use).</sub>

<br>

- ##### <return>EndPoint</return> EndPoint
  <sub>Return EndPoint (Identifies a network address. This is an ``abstract`` class).</sub>

<br>

- ##### <return>IPEndPoint</return> IPEndPoint
  <sub>Return IPEndPoint (Represents a network endpoint as an ``IP address`` and a ``port number``).</sub>

<br>

## Methods

- ##### <return>string</return> ToString()
  <sub>Return the value of endpoint as string ``IPAddress:Port``</sub>
  ```cs
  Host host = new Host("127.0.0.1", 8080);
  string value = host.ToString();
  // The value is: "127.0.0.1:8080"
  ```

<br>