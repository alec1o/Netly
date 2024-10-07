using System.Text;

namespace Netly.Interfaces
{
    public static partial class IHTTP
    {
        public interface Body
        {
            /// <summary>
            /// Body Encoding
            /// <br/> <i>If not found in HTTP header UTF-8 is used by Default</i>
            /// </summary>
            Encoding Encoding { get; }
            
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
            /// Enctype Parser: Make easy and seamless parse JSON, YML, UrlEncoded, and more!
            /// </summary>
            EnctypeParser Parser { get; }
        }
    }
}