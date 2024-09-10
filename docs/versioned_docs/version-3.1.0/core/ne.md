---
sidebar_position: 4
---


# NE (Netly Encoding)

:::tip
This Function is removed on Netly >= 4.0.0

### Solution

```cs
using Byter;

// Bytes to string
string a = byte[].GetString();
string b = byte[].GetString(Encoding.UTF8);

// String to bytes
byte[] c = string.GetBytes();
byte[] d = string.GetBytes(Encoding.UTF8);
```
:::


###### ``NE`` is ``System.Text.Encoding`` wrapper

## Properties
- ##### <return>enum</return> Mode
    <sub>Encoding protocols</sub>
    ```cs
    enum Mode { ASCII = 0, UTF7 = 1, UTF8 = 2, UTF16 = 3, UTF32 = 4, UNICODE = 5 }
    ```

<br/>

- ##### <return>NE.Mode</return> Default
    <sub>Default (explicitly) and global encoding and decoding ``default value is UTF8``</sub>


## Methods
- ##### <return>string</return> GetString(<params>byte[] buffer</params>) <br/> <return>string</return> GetString(<params>byte[] buffer</params>, <params>NE.Mode mode</params>)
    <sub>Return string from bytes (byte[])</sub>
    - ``buffer`` Is bytes (byte[]) the source that can be converted to string
    - ``mode`` Encoding protocol

<br/>

- ##### <return>string</return> GetBytes(<params>string buffer</params>) <br/> <return>string</return> GetBytes(<params>string buffer</params>, <params>NE.Mode mode</params>)
  <sub>Return bytes (byte[]) from string</sub>
  - ``buffer`` Is string the source that can be converted to bytes (byte[])
  - ``mode`` Encoding protocol
    

## Example

- Convert ``string`` to ``bytes``
    ```cs
    // *********************************************** SOURCE
    string DATA = "@alec1o";
  
    // *********************************************** NE.Default   
    byte[] data1 = NE.GetBytes(DATA);
    byte[] data2 = NE.GetBytes(DATA, NE.Default);
  
    // *********************************************** NE.Mode.UTF8 
    byte[] data3 = NE.GetBytes(DATA, NE.Mode.UTF8);
  
    // *********************************************** NE.Mode.ASCII
    string data4 = NE.GetBytes(DATA, NE.Mode.ASCII);
    ```
  
- Convert ``bytes`` to ``string``
    ```cs
    // *********************************************** SOURCE
    byte[] DATA = NE.GetBytes("@alec1o");     
  
    // *********************************************** NE.Default   
    string data1 = NE.GetString(DATA);
    string data2 = NE.GetString(DATA, NE.Default);
  
    // *********************************************** NE.Mode.UTF8 
    string data3 = NE.GetString(DATA, NE.Mode.UTF8);
  
    // *********************************************** NE.Mode.ASCII
    string data4 = NE.GetString(DATA, NE.Mode.ASCII);
    ```











