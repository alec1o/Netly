namespace Netly
{
    /// <summary>
    ///     Type of a WebSocket message.
    /// </summary>
    public enum WebSocketDelivery
    {
        /// <summary>
        ///     Binary message.
        /// </summary>
        Binary = 0,

        /// <summary>
        ///     Text message (UTF-8).
        /// </summary>
        Text = 1
    }
}
