import { themes as prismThemes } from "prism-react-renderer";
import type { Config } from "@docusaurus/types";
import type * as Preset from "@docusaurus/preset-classic";

const config: Config = {
    title: "Netly",
    tagline: "Netly: A Cross-Platform and Multi-Protocol C# Socket Library – Fast, Easy, and Versatile.⚡",
    favicon: "img/favicon.ico",

    // Set the production url of your site here
    url: "https://netly.docs.kezero.com",
    // Set the /<baseUrl>/ pathname under which your site is served
    // For GitHub pages deployment, it is often '/<projectName>/'
    baseUrl: "/",

    // GitHub pages deployment config.
    // If you aren't using GitHub pages, you don't need these.
    organizationName: "alec1o", // Usually your GitHub org/user name.
    projectName: "Netly", // Usually your repo name.

    onBrokenLinks: "throw",
    onBrokenMarkdownLinks: "warn",

    // Even if you don't use internationalization, you can use this field to set
    // useful metadata like html lang. For example, if your site is Chinese, you
    // may want to replace "en" with "zh-Hans".
    i18n: {
        defaultLocale: "en",
        locales: ["en"],
    },

    presets: [
        [
            "classic",
            {
                docs: {
                    lastVersion: "current",
                    versions: {
                        current: {
                            label: "4.0.0",
                            path: "/",
                        },
                    },
                    sidebarPath: "./sidebars.ts",
                    // Please change this to your repo.
                    // Remove this to remove the "edit this page" links.
                    editUrl: "https://github.com/alec1o/Netly/tree/dev/docs",
                },
                blog: {
                    showReadingTime: true,
                    feedOptions: {
                        type: ["rss", "atom"],
                        xslt: true,
                    },
                    // Please change this to your repo.
                    // Remove this to remove the "edit this page" links.
                    editUrl: "https://github.com/alec1o/Netly/tree/dev/docs",
                    // Useful options to enforce blogging best practices
                    onInlineTags: "warn",
                    onInlineAuthors: "warn",
                    onUntruncatedBlogPosts: "warn",
                },
                theme: {
                    customCss: "./src/css/custom.css",
                },
            } satisfies Preset.Options,
        ],
    ],

    themeConfig: {
        markdown: {
            mermaid: true,
        },
        colorMode: {
            defaultMode: "dark",
        },
        themes: ["@docusaurus/theme-mermaid"],
        // Replace with your project's social card
        image: "img/docusaurus-social-card.jpg",
        navbar: {
            title: "Netly",
            logo: {
                alt: "Netly Logo",
                src: "/img/netly-logo-3.png",
            },
            items: [
                {
                    type: "docSidebar",
                    sidebarId: "tutorialSidebar",
                    position: "left",
                    label: "Docs",
                },
                { to: "/docs/category/-api-reference", label: "API", position: "left" },
                { to: "/blog", label: "Blog", position: "left" },
                {
                    href: "https://github.com/alec1o/Netly",
                    label: "GitHub",
                    position: "right",
                },
                {
                    href: "https://www.nuget.org/packages/Netly",
                    label: "Nuget",
                    position: "right",
                },
                {
                    href: "https://assetstore.unity.com/packages/tools/network/225473",
                    label: "Unity Asset Store",
                    position: "right",
                },
                {
                    type: "docsVersionDropdown",
                },
            ],
        },
        footer: {
            style: "dark",
            links: [
                {
                    title: "Docs",
                    items: [
                        {
                            label: "API",
                            to: "/docs/overview",
                        },
                        {
                            label: "Tutorials",
                            to: "/docs/category/examples",
                        },
                        {
                            label: "🍁 TCP ㅤ ˗ˏˋ ⛉ ˎˊ˗",
                            to: "/docs/category/-tcp-examples",
                        },
                        {
                            label: "⚡ UDP",
                            to: "/docs/category/-udp-examples",
                        },
                        {
                            label: "☄️ RUDPㅤㅤ˗ˏˋ ᴍᴠᴘ ˎˊ˗",
                            to: "/docs/category/%EF%B8%8F-rudp-examples",
                        },
                        {
                            label: "🍷 HTTP",
                            to: "/docs/category/-http-examples",
                        },
                        {
                            label: "❤️‍🔥 Websocketㅤㅤ˗ˏˋ ♡︎ ˎˊ˗",
                            to: "/docs/category/%EF%B8%8F-websocket-examples",
                        },
                    ],
                },
                {
                    title: "Community",
                    items: [
                        {
                            label: "YouTube (@alec1o)",
                            href: "https://www.youtube.com/@alec1o",
                        },
                        {
                            label: "Discussions (@github)",
                            href: "https://github.com/alec1o/Netly/discussions",
                        },
                        {
                            label: "Issues (@github)",
                            href: "https://github.com/alec1o/Netly/discussions",
                        },
                        {
                            label: "Twitter (@alec1o)",
                            href: "https://twitter.com/alec1o",
                        },
                    ],
                },
                {
                    title: "More",
                    items: [
                        {
                            label: "Blog",
                            to: "/blog",
                        },
                        {
                            label: "GitHub",
                            href: "https://github.com/alec1o/Netly",
                        },
                    ],
                },
            ],
            copyright: `Copyright © ${new Date().getFullYear()} Netly, All rights reserved and Powered by. <a target="_blank" href="https://www.alecio.me">Alecio Furanze (@alec1o)</a>.`,
        },
        prism: {
            theme: prismThemes.github,
            darkTheme: prismThemes.dracula,
        },
    } satisfies Preset.ThemeConfig,
};

export default config;
