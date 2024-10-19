using System;
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
            /// Parse HTTP Body using Detected Enctype
            /// </summary>
            T Parse<T>();

            /// <summary>
            /// Parse HTTP Body using Custom Enctype
            /// </summary>
            /// <param name="enctype">Enctype Target</param>
            /// <typeparam name="T">Response Object</typeparam>
            /// <returns></returns>
            T Parse<T>(HTTP.Enctype enctype);

            /// <summary>
            /// Adding Enctype parser Method
            /// </summary>
            /// <param name="enctype">Enctype Target</param>
            /// <param name="replaceOnMatch">
            ///     <i>true:</i> Replaces the existing <i>handler</i> with this one if both target the same <i>Enctype</i>.<br/>
            ///     <i>false:</i> Uses this <i>handler</i> only if no handler for the same <i>Enctype</i> is set (does not replace an existing one).
            /// </param>
            /// <param name="handler">Target Enctype (Enctype that handler will solve)</param>
            void OnParse(HTTP.Enctype enctype, bool replaceOnMatch, Func<Type, object> handler);
        }
    }
}