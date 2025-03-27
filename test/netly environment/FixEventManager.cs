public class FixEventManager
{
    [Fact]
    public void EndToEnd()
    {
        var eventName = "Netly";
        byte[] eventData = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        var buffer = EventManager.Create(eventName, eventData);

        var result = EventManager.Verify(buffer);

        var bufferName = result.name;
        var bufferData = result.data;

        Assert.Equal(eventName, bufferName);
        Assert.Equal(eventData, bufferData);
    }
}