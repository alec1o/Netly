import clsx from "clsx";
import Heading from "@theme/Heading";
import styles from "./styles.module.css";

type FeatureItem = {
    title: string;
    img_src: string;
    description: JSX.Element;
};

const FeatureList: FeatureItem[] = [
    {
        title: "Easy to Use",
        img_src: "/img/free-code-editor-tools-bot-desk.svg",
        description: (
            <>
                Netly effortless interaction, intuitive design, and streamlined functionality come together to create a seamless user experience that
                makes it simple for anyone to get started and achieve their goals.
            </>
        ),
    },
    {
        title: "Focus on What Matters",
        img_src: "/img/swimlane-welcome-to-the-community.svg",
        description: (
            <>
                Leave the intricacies of sockets and network management to Netly, so you can focus on what really matters - building your application.
                With a plethora of protocols and features at your disposal, you can concentrate on writing code that drives business value, rather
                than getting bogged down in low-level socket and networking complexity..
            </>
        ),
    },
    {
        title: "Powered by C#",
        img_src: "/img/swimlane_xamarin_cross_platform.svg",
        description: (
            <>
                Netly seamlessly integrates with your C# application, including popular frameworks and libraries, and is particularly well-suited for
                high-performance applications such as those built with game engines like Unity, Flax, and others. Easily harness the power of Netly to
                build scalable, high-performance networking solutions, and focus on creating exceptional user experiences..
            </>
        ),
    },
];

function Feature({ title, img_src, description }: FeatureItem) {
    return (
        <div className={clsx("col col--4")}>
            <div className="text--center">
                <img src={img_src} alt="" />
            </div>
            <div className="text--center padding-horiz--md">
                <Heading as="h3">{title}</Heading>
                <p>{description}</p>
            </div>
        </div>
    );
}

export default function HomepageFeatures(): JSX.Element {
    return (
        <section className={styles.features}>
            <div className="container">
                <div className="row">
                    {FeatureList.map((props, idx) => (
                        <Feature key={idx} {...props} />
                    ))}
                </div>
            </div>
        </section>
    );
}
