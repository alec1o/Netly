namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        internal interface EnctypeObject
        {
            bool IsNull { get; }
            string String { get; }
            byte[] Bytes { get; }
        }
    }
}