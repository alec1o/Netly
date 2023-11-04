using System.Net.WebSockets;

namespace Netly.Core
{
    internal static class BufferTypeWrapper
    {
        internal static WebSocketMessageType ToWebsocketMessageType(BufferType type)
        {
            switch (type)
            {
                case BufferType.Binary: return WebSocketMessageType.Binary;
                case BufferType.Text: return WebSocketMessageType.Text;
                default:
                    // Well, Now all other options our gonna return as BinaryType because is most generic that TextType
                    return WebSocketMessageType.Binary;
            }
        }

        internal static BufferType FromWebsocketMessageType(WebSocketMessageType type)
        {
            switch (type)
            {
                case WebSocketMessageType.Binary: return BufferType.Binary;
                case WebSocketMessageType.Text: return BufferType.Text;
                default:
                    // Well, Now all other options our gonna return as BinaryType because is most generic that TextType
                    return BufferType.Binary;
            }
        }
    }
}