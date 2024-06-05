using Netly;

namespace test;


public class MessageParserTest
{
    [Fact]
    public void EndToEnd()
    {
        string eventName = "sample";
        byte[] eventData = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        byte[] buffer = NetlyEnvironment.EventManager.Create(eventName, eventData);

        var result = NetlyEnvironment.EventManager.Verify(buffer);

        string bufferName = result.name;
        byte[] bufferData = result.data;

        Assert.Equal(eventName, bufferName);
        Assert.Equal(eventData, bufferData);
    }
}