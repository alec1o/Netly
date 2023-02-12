<h1 align="center"><a href="https://github.com/kezerocom">Kezero</a> Netly</h1>
<h5 align="center">  
  <img src="content/logo/netly-logo-2.png" width="200px">
  <img src="content/logo/netly-logo-3.png" width="200px">
</h5>

## About

> Netly is a low-level socket library whose main function is to make socket usage easier, readable and productive. Its syntax is minimalistic and inspired by [socket.io](https://socket.io). It supports using TCP/UDP and Client/Server. The reason for the emergence was aimed at building a multiplayer game server.

<br>

## Documentation
> Official documentation

- Reference documentation generated from source code using the [DocFx](https://dotnet.github.io/docfx) tool: [See here.](http://docs-netly.kezero.com)

- Documentation based on sample projects, hosted on [Gitbook](https://gitbook.com) tool: [See here.](https://netly.kezero.com)

### Overview  
  - TCP
    - [Client](/Docs/TCP/Client.md)
    - [Server](/Docs/TCP/Server.md)
  - UDP
    - [Client](/Docs/UDP/Client.md)
    - [Server](/Docs/UDP/Server.md)
  - CORE
    - [Async](/Docs/CORE/Async.md)
    - [x] [Call](/Docs/CORE/Call.md)
    - [Compare](/Docs/CORE/Compare.md)
    - [Compress](/Docs/CORE/Compress.md)
    - [Concat](/Docs/CORE/Concat.md)
    - [Dict](/Docs/CORE/Dict.md)
    - [Encode](/Docs/CORE/Encode.md)
    - [Events](/Docs/CORE/Events.md)
    - [Host](/Docs/CORE/Host.md)
  

<br>

## Package manager
> This library can be found in some package managers, see below

- Nuget / [SEE HERE]() (not found)
- Unity AssetStore / [SEE HERE](https://assetstore.unity.com/packages/tools/network/225473)


<br>

## Platforms

### Build Platforms (RUNTIME)

### Note
> Netly is a native script and runs on all .NET C# projects (e.g: Console Application, Xamarin) or projects that contain dotnet C# as a script (e.g: Game Engine), as described below some of the famous platforms that netly can be used.

### Attention
> Despite running on platforms with C# as base or scripting language, it can be installed in different ways, e.g:
- In Unity Engine (we can just add the dlls already compiled in a directory in the unity project and it will already be installed);
- In the Console Application (We can add the dlls link as a project reference in the .csproj file and it will also be already there)
- Nuget (We can also use the dotnet package manager "Nuget" to be able to add/install it in our project. It is the most recommended if possible)

### Summary
> We saw that we can install it in several places, but the installation can vary a lot from platform, but after the installation the usability is the same (Simple, Minimalist, and Robust).

### Platform List
- [.NET](https://dotnet.microsoft.com) (SDK)
- [Mono](https://mono-project.com) (SDK)
- [Xamarin](https://dotnet.microsoft.com/xamarin) (SDK)
- [Unity](https://unity.com) (Engine)
- [FlaxEngine](https://flaxengine.com) (Engine)
- [MonoGame](https://monogame.net) (Engine)
- [Stride](https://stride3d.net) (Engine)
- [CryEngine](https://cryengine.com) (Engine)

<br>

## Last version
> Below are some of the highlighted feature added in the current version

- [x] TCP client/server working fine.
- [x] Creating (dic) to pack and unpack data easily.
- [x] UDP client/server creation worked, but not finished.

<br>

## Currently missing feature
> Below are some missing features that are planned to be added in later versions.

- [ ] [TCP Client/Server] Support SSL/TLS encryption.
- [ ] [TCP Client] Add auto-reconnect and reconnect timeout
- [ ] [UDP client/server] Detect disconnected connection (using continuous pings and a no-response timeout to identify client-server connection).
- [ ] [Core] Add other data compression system besides GZIP which is already implemented.
- [ ] [Core] Add new binary encoding type like UTF-7, UTF-16, UTF-32.
- [x] [TCP/UDP client/server] Send data in string without having to convert from string to bytes manually.

<br>

## Copyright
- Maintainer: KEZERO / [@kezerocom](https://github.com/kezerocom)
- Developed by: ALECIO FURANZE / [@alec1o](https://github.com/ALEC1O)
- Support: EMAIL / [CONTACT HERE](mailto://support@kezero.com)
- License: MIT / [SEE HERE](/LICENSE.md)
