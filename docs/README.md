<h3 align='center'><a href="https://netly.docs.kezero.com">Netly docs</a></h3>

#### Dependencies
* [.NET](http://dotnet.microsoft.com/) <sup><i>(+8 LTS)</i></sup>
* [DocFx](https://github.com/dotnet/docfx)

#### Commands
```rb
# Installing DocFx
$ dotnet tool update -g docfx

# Run project in development mode (docfx don't have watch mode)
$ docfx docfx.json --serve

# Build documentation for production
$ docfx build docfx.json --output <path>
```
