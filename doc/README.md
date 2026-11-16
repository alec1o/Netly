### Netly Documentation Website

### Commands

-   ##### Install dependencies:

    ```
    pnpm install

    dotnet tool install -g docfx

    dotnet tool install -g DocFxMarkdownGen
    ```

-   ##### Start the development server:
    ```
    pnpm start
    ```

### Generate API

```bash
# DocFx
docfx

# FxMarkdownGen
dfmg

# config
rm docs/api/index.md
cp docs.api._category_.json docs/api/_category_.json
    # in workflow
    rm docs/.gitignore
```

-   ##### Dependencies
    -   [Docusaurus](https://github.com/facebook/docusaurus)
    -   [DocFx](https://github.com/dotnet/docfx)
    -   [DocFxMarkdownGen](https://github.com/Jan0660/DocFxMarkdownGen/)
