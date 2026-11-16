---
sidebar_position: 3
---


# MessageFraming

###### Message framing implementation


## Namespace
```cs
using Netly.Core;
```

<br/>

## Constructors
- ##### <return>MessageFraming</return> MessageFraming()
	<sub> Create ``Message Framing`` instance</sub>

<br/>


## Properties
- ##### <return>byte[]</return> PREFIX
    <sub>Return ``netly`` framing prefix</sub>
    ```cs
    [0, 8, 16, 32, 64, 128 ]
    ```

- ##### <return>int</return> MaxSize
    <sub>Return ``max`` buffer size that can be received is bytes, default value is ``8MB``</sub>
    - <return>Warning</return> Connection will disconnected after receive more big that data, for prevent bad work.

<br/>

- ##### <return>int</return> UdpBuffer
    <sub>Return ``max`` udp package, default is ``1MB``</sub>


<br/>


## Methods
> Triggers

- ##### <return>void</return> Add(<params>byte[] buffer</params>)
  <sub>Add buffer in flow.</sub>
  - <sub>``buffer`` Is a data received (complete or half)</sub>

<br/>

- ##### <return>void</return> Clear()
  <sub>Clean internal buffer for release memory (reset object, ``do not reset callback's handler``).</sub>

<br/>

- ##### <return>static</return> <return>byte[]</return> CreateMessage(<params>byte[] value</params>)
  <sub>Generate framed buffer from custom input</sub>
  - <sub>``value`` is the target data.</sub>
  - <return>return</return> <sub>return the generated/created buffer.</sub>




> Callback

- ##### <return>void</return> OnData(<params>Action&lt;byte[]&gt; callback</params>)
  <sub>Called when receive parsed or complete/processed data.</sub>
  - <sub>``callback`` Callback that will be called for handle this event</sub>

<br/>

- ##### <return>void</return> OnError(<params>Action&lt;Exception&gt; callback</params>)
  <sub>Called when receive some error on ``message framing``. on this step's TCP/SSL auto disconnect for prevent bad works (because received invalid)</sub>
  - <sub>``callback`` Callback that will be called for handle this event</sub>

<br/>

<br/>




# Explication

##### README <sub>https://web.archive.org/web/20230219220947/https://blog.stephencleary.com/2009/04/message-framing.html</sub>


## What is Message Framing
Message framing is a technique that an application uses to organize received data in order to obtain the same hash as the sent data.


## Why use Message Framing
A TCP/IP application has a data limit that is covered and this value is very small, around 8kb. I ask you a question! If the maximum capacity of a packet is 8kb, how can I send or receive 1MB or 1GB? It is necessary for the party that will receive the data to create a buffer that will package the data until it reaches the target, which can be 1MB or 1GB or any other value.


## What is Netly Message Framing API
```cs

byte[] NETLY_PREFIX = new byte[] { 0, 8, 16, 32, 64, 128 }; // 6 Bytes
byte[] BUFFER_DATA = NE.GetString("Hello world!"); // Dynamic size
byte[] BUFFER_SIZE = BitConverter.GetBytes(BUFFER_DATA.Lenght); // 4 Bytes

// DATA: NETLY_PREFIX + BUFFER_SIZE + BUFFER_DATA;
byte[] DATA = JOIN_BUFFER();

// Concat buffers for create a valid Netly data
byte[] JOIN_BUFFER()
{
	return new byte[3][] { NETLY_PREFIX, BUFFER_SIZE, BUFFER_DATA }.SelectMany(x => x).ToArray();
}
```

## Customize Message Framing
No, now ``netly`` don't have a callback for create custom validation and verification, for customize ``MessageFraming``

##### So Cute  :kissing: :kissing_cat: :kissing_closed_eyes: :kissing_heart :kissing_smiling_eyes:
- Like this (Maybe on future) ``Custom Message Framing Protocol``
	```cs
	MessageFraming.Customize((List<byte> buffer, byte[] received, object anyData) =>
	{
	  LOAD_DATA:

		MyClass mydb;
		buffer.AddRange(received);

		if (anyData == null)
		{
			mydb = new MyClass();
			mydb.offset = 0;
			mydb.prefix = new byte[] { };
			mydb.len = 0;
			mydb.init = false;
			anyData = mydb; // backup state data
		}
		else
		{
			mydb = (MyClass)anyData;
		}

		if (mydb.len == 0)
		{
			if (buffer.Count >= 4)
			{
				mydb.len = BitConverter.ToInt32(buffer.ToArray(), index: 0);
				buffer.RemoveRange(0, sizeof(int));
				mydb.init = true;
			}
		}
		else
		{
			if (buffer.Count > 0 && buffer.Count >= mydb.len)
			{
				byte[] mydata = buffer.GetRange(0, mydb.len);
				buffer.RemoveRange(0, mydb.len);

				InvokeData(mydata);

				if (buffer.Count > 0)
				{
					goto LOAD_DATA;
				}
			} 
		}

		void InvokeData(byte[] data)
		{
			Console.WriteLine($"Received: {NE.GetString(data, NE.Mode.ASCII)}");
		}
	});
	```