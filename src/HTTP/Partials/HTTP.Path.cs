using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Netly
{
    public partial class HTTP
    {
        internal static class Path
        {
            /// <summary>
            /// Validate regular path regex e.g.: /root/path/
            /// </summary>
            public static readonly Regex ValidateRegularPathRegex;

            /// <summary>
            /// Validate param path regex e.g.: /root/home/{user}/code/{file}/
            /// </summary>
            public static readonly Regex ValidateParamPathRegex;

            /// <summary>
            /// Validate param field e.g.: {user}, {file}
            /// </summary>
            public static readonly Regex ValidateParamFieldRegex;

            static Path()
            {
                RegexOptions options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant;
                ValidateRegularPathRegex = new Regex("^([/][a-zA-Z0-9-_@]+)([/][a-zA-Z0-9-_@]+)*([/]?)?", options);
                ValidateParamPathRegex = new Regex("^(([/]([{][[a-zA-Z0-9-._@]*[}])+)|([/][a-zA-Z0-9-._@]+))*[/]?", options);
                ValidateParamFieldRegex = new Regex("^({)[(a-zA-Z)]+[\\d]*(})", options);
            }

            public static bool IsValid(string path)
            {
                return ValidateRegularPathRegex.IsMatch(path) || ValidateParamPathRegex.IsMatch(path);
            }

            public static bool ComparePath(string origin, string input)
            {
                var a = origin.Trim() ?? string.Empty;
                var b = input.Trim() ?? string.Empty;

                if (a.Equals(b)) return true;

                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;

                AddEndOfPath(ref a);
                AddEndOfPath(ref b);

                return a.Equals(b);
            }

            private static void AddEndOfPath(ref string path)
            {
                const char endOfPathKey = '/';

                if (path[path.Length - 1] != endOfPathKey) path += endOfPathKey;
            }
        }
    }
}