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
            ///     Parses the HTTP body into the specified type.
            /// </summary>
            /// <typeparam name="T">The type to parse the body into.</typeparam>
            /// <returns>An instance of <typeparamref name="T"/> containing the parsed data.</returns>
            T Parse<T>();

            /// <summary>
            ///     Registers a parser for handling HTTP body content based on its request instance.
            /// </summary>
            /// <param name="replaceOnMatch">
            ///     <c>true</c> to replace an existing parser if one is already registered for the same request instance;<br/>
            ///     <c>false</c> to register the parser only if no existing parser is set for that request instance.
            /// </param>
            /// <param name="parser">
            ///     A function that takes a <see cref="Type"/> and returns an <see cref="object"/> 
            ///     representing the parsed content.
            /// </param>
            void RegisterParser(bool replaceOnMatch, Func<Type, object> parser);
        }
    }
}