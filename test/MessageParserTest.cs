namespace test;

using Netly.Core;

public class MessageParserTest
{
    [Fact]
    public void EndToEnd()
    {
        string eventName = "sample";
        byte[] eventData = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        byte[] buffer = EventManager.Create(eventName, eventData);

        var result = EventManager.Verify(buffer);

        string bufferName = result.name;
        byte[] bufferData = result.data;

        Assert.Equal(eventName, bufferName);
        Assert.Equal(eventData, bufferData);
    }
}