using System.Collections.Generic;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface Body
        {
            /// <summary>
            ///     Enctype type
            /// </summary>
            HTTP.Enctype Enctype { get; }

            /// <summary>
            ///     Text buffer
            /// </summary>
            string Text { get; }

            /// <summary>
            ///     Binary buffer
            /// </summary>
            byte[] Binary { get; }

            /// <summary>
            ///     Get value from Enctype content (return string)
            /// </summary>
            Dictionary<string, string> TextForm { get; }

            /// <summary>
            ///     Get value from Enctype content (return bytes)
            /// </summary>
            Dictionary<string, byte[]> BinaryForm { get; }
        }
    }
}