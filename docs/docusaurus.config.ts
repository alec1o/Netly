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
                    sidebarPath: "./sidebars.ts",
                    // Please change this to your repo.
                    // Remove this to remove the "edit this page" links.
                    editUrl: "https://github.com/alec1o/Netly/tree/dev",
                },
                blog: {
                    showReadingTime: true,
                    feedOptions: {
                        type: ["rss", "atom"],
                        xslt: true,
                    },
                    // Please change this to your repo.
                    // Remove this to remove the "edit this page" links.
                    editUrl: "https://github.com/alec1o/Netly/tree/dev",
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
                { to: "/blog", label: "Blog", position: "left" },
                {
                    href: "https://github.com/alec1o/Netly",
                    label: "GitHub",
                    position: "right",
                },
                /* 
                {
                    type: "docsVersionDropdown",
                },
                */
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
                            label: " </>  Websocket",
                            to: "/docs/category/websocket-examples",
                        },
                        {
                            label: " </>  RUDP",
                            to: "/docs/category/rudp-examples",
                        },
                        {
                            label: " </>  HTTP",
                            to: "/docs/category/http-examples",
                        },
                        {
                            label: " </>  TCP",
                            to: "/docs/category/tcp-examples",
                        },
                        {
                            label: " </>  UDP",
                            to: "/docs/category/udp-examples",
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
            copyright: `Copyright © ${new Date().getFullYear()} Netly, All rights reserved and Powered by. <a href="https://www.alecio.me">Alecio Furanze (@alec1o)</a>.`,
        },
        prism: {
            theme: prismThemes.github,
            darkTheme: prismThemes.dracula,
        },
    } satisfies Preset.ThemeConfig,
};

export default config;
