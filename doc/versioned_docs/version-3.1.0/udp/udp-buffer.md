---
sidebar_position: 3
---


# UdpBuffer

:::warning
This has removed on Netly >= 4.0.0
:::

## Udp Buffer
The Udp buffer is the maximum amount of data (bytes) that udp will receive.
The ``UdpServer`` instance uses a single buffer instance and ``UdpServer`` uses the same buffer to receive all data sent by clients.
The ``UdpClient`` instance uses a single buffer. And the ``UdpClient`` from ``server-side`` does not use a buffer because the server itself receives the data.


<return>To update the default buffer value you can use the example code below:</return><br/>
<params>You must update the buffer size before connecting to the UdpClient/UdpServer. so that the predefined buffer is used.</params>
## Default Value
``buffer`` is bytes (byte[]) and default value is ``1mb`` (1.048.576 bytes)

## Update udp buffer size
```cs
using Netly.Core;

const int MB = 1024 * 1024;
MessageFraming.UdpBuffer = MB * 2; // 2 megabytes
```