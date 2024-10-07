namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface EnctypeParser
        {
            bool IsValid { get; }
            string[] Keys { get; }
            EnctypeObject this[string key] { get; }
        }
    }
}