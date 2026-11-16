namespace Netly
{
    public static partial class HTTP
    {
        /// <summary>
        ///     Websocket Message Type
        /// </summary>
        public enum MessageType
        {
            Binary = 0,
            Text = 1
        }


        /// <summary>
        ///     Binary Websocket Message Type
        /// </summary>
        public static readonly MessageType Binary = MessageType.Binary;
        
        /// <summary>
        ///     Text Websocket Message Type
        /// </summary>
        public static readonly MessageType Text = MessageType.Text;
    }
}