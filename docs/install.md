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
# 1. clone project
$ git clone https://github.com/alec1o/Netly netly 

# 2. build project
$ dotnet build netly/ -c Release -o netly/bin/

# NOTE:
# Netly.dll require Byter.dll because is Netly dependency
# Netly.dll and Byter.dll have on build folder <netly-path>/bin/
```
> You might use Netly.dll & Byter.dll on your project, e.g: you may use netly with language like (java, c++ or etc).
