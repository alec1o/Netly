### Netly Documentation Website

### Commands

-   ##### Install dependencies:

    ```
    pnpm install

    dotnet tool install -g docfx

    dotnet tool install -g dfmg
    ```

-   ##### Start the development server:
    ```
    pnpm start
    ```

### Generate API

    ```sh
    # DocFx
    docfx

    # FxMarkdownGen
    dfmg

    # config
    rm docs/api/index.md
    cp docs.api._category_.json docs/api/_category_.json
    ```

-   ##### Dependencies
    -   [Docusaurus](https://github.com/facebook/docusaurus)
    -   [DocFx](https://github.com/dotnet/docfx)
    -   [DocFxMarkdownGen](https://github.com/Jan0660/DocFxMarkdownGen/)
