using System.Security.Cryptography.X509Certificates;

namespace test;
using Netly.Core;

public class MessageParserTest
{
    [Fact]
    public void EndToEnd()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        byte[] sendBuffer = MessageParser.SetPrefix(data);

        List<byte[]> receivedMessages = MessageParser.GetMessages(sendBuffer);

        int _size = receivedMessages.Count;

        Assert.Equal(1, _size);
        Assert.Equal(10, receivedMessages[0].Length);
    }

    [Fact]
    public void EndToEndMultMessage()
    {
        byte[] data1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] data2 = { 0, 1, 2, 3, 4, };
        byte[] data3 = { 0, 1, 2, 3, 4, 5, 6, 7 };

        byte[] sendBuffer1 = MessageParser.SetPrefix(data1);
        byte[] sendBuffer2 = MessageParser.SetPrefix(data2);
        byte[] sendBuffer3 = MessageParser.SetPrefix(data3);

        List<byte[]> buffers = new List<byte[]> { sendBuffer1, sendBuffer2, sendBuffer3 };

        List<byte[]> receivedMessages = MessageParser.GetMessages(buffers.SelectMany(x => x).ToArray());

        int _size = receivedMessages.Count;

        Assert.Equal(3, _size);
        Assert.Equal(9, receivedMessages[0].Length);
        Assert.Equal(5, receivedMessages[1].Length);
        Assert.Equal(8, receivedMessages[2].Length);

        Assert.Equal(data1, receivedMessages[0]);
        Assert.Equal(data2, receivedMessages[1]);
        Assert.Equal(data3, receivedMessages[2]);
    }
}

