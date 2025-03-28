---
sidebar_position: 2
---

# Installing

On this session we will learn how to install Netly in your project.

### Package Manager

-   #### Nuget <a target="_blank" href="https://nuget.org/packages/Netly"><sup>Netly On Nuget (website)</sup></a>

        ```bash --title=".NET CLI (Terminal)"
        dotnet add package Netly --version 4.0.0
        ```
        ```bash --title="Package Manager (Visual Studio)"
        NuGet\Install-Package Netly -Version 4.0.0
        ```

<br/>

-   #### Unity Asset Store:
    _Install Netly in Unity Asset Store by. [Netly in Unity Asset Store](https://assetstore.unity.com/packages/tools/network/225473)_

<br/>

### Build Netly from source code (dll's)

    - #### Dependencies
        - [Git](https://git-scm.com)
        - [.NET](https://dotnet.microsoft.com)

    - #### Build Steps
        ```bash
        # clone Netly repo or download
        git clone https://github.com/alec1o/Netly Netly

        # enter in Netly folder
        cd Netly

        # build Netly project
        dotnet build -o bin

        # enter in bin folder (contain Netly.dll and Byter.dll)
        cd bin/


        # you should see Netly.dll and Byter.dll
        ls; dir; # use ls or dir on windows when standard CMD environment
        ```

        :::warning
        When using Netly in your project, please note that it requires Byter.dll or the source code <a target="_blank" href="https://github.com/alec1o/Byter"><sup>(Byter source code in GitHub)</sup></a>. Make sure to include Byter in your project to avoid build errors.

        Byter.dll is automatically included when you download Netly from the Unity Asset Store or NuGet. If you're building Netly manually, Byter.dll will be generated in the same directory as Netly.dll.
        :::
