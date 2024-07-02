namespace Netly
{
    public static partial class RUDP
    {
        public const MessageType Reliable = MessageType.Reliable;
        public const MessageType ReliableOrdered = MessageType.ReliableOrdered;
        public const MessageType Unreliable = MessageType.Unreliable;
        public const MessageType UnreliableOrdered = MessageType.UnreliableOrdered;

        public enum MessageType : sbyte
        {
            Reliable = 1,
            ReliableOrdered = -1,
            Unreliable = 2,
            UnreliableOrdered = -2,
        }
    }
}