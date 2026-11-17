namespace Netly
{
    internal interface IHttpEnctypeParser
    {
        bool IsValid { get; }
        string[] Keys { get; }
        IHttpEnctypeObject this[string key] { get; }
    }

    internal interface IHttpEnctypeObject
    {
        bool IsNull { get; }
        string String { get; }
        byte[] Bytes { get; }
    }
}
