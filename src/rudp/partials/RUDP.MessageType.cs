namespace Netly
{
    public static partial class RUDP
    {
        public const MessageType Sequenced = MessageType.Sequenced;
        public const MessageType Unreliable = MessageType.Unreliable;
        public const MessageType Reliable = MessageType.Reliable;

        public enum MessageType : byte
        {
            /// <summary>
            ///     Received Unordered and isn't reliable
            /// </summary>
            Unreliable = 0,

            /// <summary>
            ///     Received Ordered and isn't reliable
            /// </summary>
            Sequenced = 111,

            /// <summary>
            ///     Received Ordered and is reliable
            /// </summary>
            Reliable = 222,
        }
    }
}