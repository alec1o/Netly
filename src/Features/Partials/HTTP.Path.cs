using System;
using System.Text.RegularExpressions;

namespace Netly.Features
{
    public partial class HTTP
    {
        public static class Path
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
                return origin.Trim().Equals(input.Trim());
            }
        }
    }
}