namespace NetlyTest.Netly;

public class NUtilsTest
{
    [Fact]
    public void SegmentShift_ShiftsCorrectly()
    {
        var array = new[] { 1, 2, 3, 4, 5 };
        var segment = new ArraySegment<int>(array, 0, array.Length);

        var shifted = NUtils.SegmentShift(segment, 2);

        Assert.Equal(3, shifted.Count);
        Assert.Equal(3, shifted.Array![shifted.Offset]);
        Assert.Equal(4, shifted.Array[shifted.Offset + 1]);
        Assert.Equal(5, shifted.Array[shifted.Offset + 2]);
    }

    [Fact]
    public void SegmentShift_ShiftGreaterThanCount_ReturnsEmpty()
    {
        var array = new[] { 1, 2, 3 };
        var segment = new ArraySegment<int>(array);

        var shifted = NUtils.SegmentShift(segment, 5);

        Assert.Equal(0, shifted.Count);
        Assert.Equal(array, shifted.Array);
        Assert.Equal(segment.Offset + segment.Count, shifted.Offset);
    }

    [Fact]
    public void SegmentShift_NegativeShift_Throws()
    {
        var array = new[] { 1, 2 };
        var segment = new ArraySegment<int>(array);
        Assert.Throws<ArgumentOutOfRangeException>(() => NUtils.SegmentShift(segment, -1));
    }

    [Fact]
    public void ArraySequenced_AllEqual_ReturnsTrue()
    {
        var a1 = new[] { 1, 2, 3 };
        var a2 = new[] { 1, 2, 3 };
        var a3 = new[] { 1, 2, 3 };

        var result = NUtils.ArraySequenced(a1, a2, a3);
        Assert.True(result);
    }

    [Fact]
    public void ArraySequenced_NotAllEqual_ReturnsFalse()
    {
        var a1 = new[] { 1, 2 };
        var a2 = new[] { 1, 3 };

        var result = NUtils.ArraySequenced(a1, a2);
        Assert.False(result);
    }

    [Fact]
    public void Format_ReturnsCorrectString()
    {
        var arr = new[] { 1, 2, 3 };
        var result = NUtils.Format(arr);
        Assert.Equal("[1,2,3]", result);
    }

    [Fact]
    public void ArrayConcat_ConcatenatesArrays()
    {
        var a1 = new[] { 1, 2 };
        var a2 = new[] { 3, 4 };
        var a3 = new[] { 5 };

        var result = NUtils.ArrayConcat(a1, a2, a3);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public void ArrayConcat_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => NUtils.ArrayConcat<int>(null));
    }

    [Fact]
    public void NewStream_SizeUnderLimit_CreatesMemoryStream()
    {
        var size = 1024 * 1024 * 20; // 20 MB
        using var stream = NUtils.NewStream(size);
        Assert.IsType<MemoryStream>(stream);
        Assert.Equal(size, ((MemoryStream)stream).Capacity);
    }

    [Fact]
    public void NewStream_SizeOverLimit_Throws()
    {
        var size = 1024 * 1024 * 21; // 21 MB
        Assert.Throws<InternalBufferOverflowException>(() => NUtils.NewStream(size));
    }
}