using System;
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

            /// <summary>
            /// Regex Timout. It prevent attacks
            /// </summary>
            public const int RegexTimeout = 5000;

            static Path()
            {
                RegexOptions options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant;
                TimeSpan timeout = TimeSpan.FromMilliseconds(RegexTimeout);

                ValidateRegularPathRegex =
                    new Regex("^([/][a-zA-Z0-9-_@]+)([/][a-zA-Z0-9-_@]+)*([/]?)?", options, timeout);
                ValidateParamPathRegex = new Regex("^(([/]([{][[a-zA-Z0-9-._@]*[}])+)|([/][a-zA-Z0-9-._@]+))*[/]?",
                    options, timeout);
                ValidateParamFieldRegex = new Regex("^({)[(a-zA-Z)]+[\\d]*(})", options, timeout);
            }

            public static bool IsValid(string path)
            {
                string value = (path ?? string.Empty).Trim();

                // Prevent Regex -> ArgumentNullException 
                if (string.IsNullOrWhiteSpace(value)) return false;

                // Set last '/' if not exist
                AddEndOfPath(ref value);

                try
                {
                    return ValidateRegularPathRegex.IsMatch(value) || ValidateParamPathRegex.IsMatch(value);
                }
                catch (RegexMatchTimeoutException e)
                {
                    // Prevent Regex -> RegexMatchTimeoutException (Regex Attack)
                    return false;
                }
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

            public static void AddEndOfPath(ref string path)
            {
                const char endOfPathKey = '/';

                if (path[path.Length - 1] != endOfPathKey) path += endOfPathKey;
            }
        }
    }
}