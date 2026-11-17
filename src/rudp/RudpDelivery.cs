namespace Netly
{
    /// <summary>
    ///     How a RUDP message is delivered.
    /// </summary>
    public enum RudpDelivery : byte
    {
        /// <summary>
        ///     Not ordered and not guaranteed.
        /// </summary>
        Unreliable = 0,

        /// <summary>
        ///     Ordered (old ones are dropped) and not guaranteed.
        /// </summary>
        Sequenced = 111,

        /// <summary>
        ///     Ordered and guaranteed.
        /// </summary>
        Reliable = 222
    }
}
