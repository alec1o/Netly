# Install Netly
For install netly you need use ``package manager`` or compile source code.

## Unity Asset store
> [!INFO]
> For install netly library on unity application you have 3 way for do it: 1th use ``unity package manager (asset store)``, 2th include ``netly dll`` on your unity project and [must include ``Byter dll``, see here](https://github.com/alec1o/Byter), or 3th include netly source code on your projeto, for this case ``I you recommend use git submodule.``

> [Netly on unity asset store, here](https://assetstore.unity.com/packages/tools/network/225473)

## Nuget
```bash
$ dotnet add package netly
# or
$ dotnet add package netly --version 3.0.0
```
> [Netly on nuget (C# package manager), here](https://www.nuget.org/packages/Netly)


# Build source code
```bash
# Build netly project
$ git clone https://github.com/alec1o/Netly netly
$ dotnet build -c Release netly/
# --- copy netly dll
$ mkdir output && mv netly/src/bin/Release/netstandard2.0/Netly.dll output/ 

# Build byter project (netly dependecy)
$ git clone https://github.com/alec1o/Byter byter
$ dotnet build -c Release byter/  
# --- copy byter dll
$ mv byter/src/bin/Release/netstandard2.0/Byter.dll  output/ 

```
> At this time you have Netly.dll and Byter.dll in output folder, you can use dlls for add on Unity projet (is a example), anyway you have dlls for use! ``(*_*)``
