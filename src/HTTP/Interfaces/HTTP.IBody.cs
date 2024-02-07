using System.Collections.Generic;
using Netly.Core;

namespace Netly
{
    public static partial class HTTP
    {
        public interface IBody
        {
            /// <summary>
            /// Enctype type
            /// </summary>
            Enctype Enctype { get; }

            /// <summary>
            /// Text buffer
            /// </summary>
            string Text { get; }

            /// <summary>
            /// Binary buffer
            /// </summary>
            byte[] Binary { get; }

            /// <summary>
            /// Get value from Enctype content (return string)
            /// </summary>
            Dictionary<string, string> TextQueries { get; }

            /// <summary>
            /// Get value from Enctype content (return bytes)
            /// </summary>
            Dictionary<string, byte[]> BinaryQueries { get; }
        }
    }
}