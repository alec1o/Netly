using System.Text.RegularExpressions;

namespace Netly.Features
{
    public partial class HTTP
    {
        internal static class Path
        {
            public static bool IsValid(string path)
            {
                // TODO: Create unique regex for validate both scenarios
                // Validate regular path regex (e.g. /root/path[/]?)
                const string regular = "^([/][a-zA-Z0-9-_@]+)([/][a-zA-Z0-9-_@]+)*([/]?)?";
                // Validate param path regex (e.g. /root/{name}/{id}[/]?):
                const string nonRegular = "^(([/]([{][[a-zA-Z0-9-._@]*[}])+)|([/][a-zA-Z0-9-._@]+))*[/]?";

                return
                (
                    Regex.IsMatch(path, regular, RegexOptions.ECMAScript)
                    ||
                    Regex.IsMatch(path, nonRegular, RegexOptions.ECMAScript)
                );
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
                
                if (path[path.Length - 1] != endOfPathKey)
                {
                    path += endOfPathKey;
                }
            }
        }
    }
}