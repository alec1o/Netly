using System;
using System.Collections.Generic;
using System.Linq;
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

            /// <summary>
            /// Param Parsing Result
            /// </summary>
            public struct ParseResult
            {
                public bool Valid { get; private set; }
                public KeyValuePair<string, string>[] Params { get; private set; }

                public ParseResult(bool valid = false, KeyValuePair<string, string>[] @params = null)
                {
                    Valid = valid;
                    Params = @params ?? Array.Empty<KeyValuePair<string, string>>();
                }
            }

            static Path()
            {
                RegexOptions options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant;
                TimeSpan timeout = TimeSpan.FromMilliseconds(RegexTimeout);

                ValidateRegularPathRegex =
                    new Regex("^([/][a-zA-Z0-9-._@]+)([/][a-zA-Z0-9-._@]+)*([/]?)?$", options, timeout);

                ValidateParamPathRegex =
                    new Regex("^(([/]([{][[a-zA-Z0-9-._@]*[}])+)|([/][a-zA-Z0-9-._@]+))*[/]?$", options, timeout);

                ValidateParamFieldRegex =
                    new Regex("^{[(a-zA-Z0-9)]+}$", options, timeout);
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
                    return IsRegularPath(path) || IsParamPath(path);
                }
                catch (RegexMatchTimeoutException e)
                {
                    // Prevent Regex -> RegexMatchTimeoutException (Regex Attack)
                    Netly.Logger.PushError(e);
                    return false;
                }
            }

            public static bool IsRegularPath(string path)
            {
                path = (path ?? string.Empty).Trim();

                if (string.Equals(path, "/")) return true;

                if (string.IsNullOrWhiteSpace(path)) return false;

                AddEndOfPath(ref path);

                return ValidateRegularPathRegex.IsMatch(path);
            }

            public static bool IsParamPath(string path)
            {
                path = (path ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(path)) return false;

                AddEndOfPath(ref path);

                return ValidateParamPathRegex.IsMatch(path) && !IsRegularPath(path);
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

            /// <summary>
            /// Parse custom path to get params
            /// </summary>
            /// <param name="originalPath">Path custom: e.g.? /root/{folder}/</param>
            /// <param name="inputPath">Absolute path: e.g.: /root/home/</param>
            /// <returns></returns>
            public static ParseResult ParseParam(string originalPath, string inputPath)
            {
                // VALIDATE INPUTS
                string path = (originalPath ?? String.Empty).Trim();
                // is path but this path don't contain especial keys ("{" and "}")
                string input = (inputPath ?? String.Empty).Trim();

                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(input)) return new ParseResult();

                AddEndOfPath(ref path);
                AddEndOfPath(ref input);

                // the path and input-path must not be same e.g.: /root/{folder}/ and /root/home
                if (path == input) return new ParseResult();

                // +++ CHECK path SYNTAX
                // invalid path syntax. it must contain one or more especial e.g.: /root/{user}/files/{filename}/
                if (!IsParamPath(path)) return new ParseResult();
                // invalid input-path syntax. it must be regular path e.g.: /root/home/files.however/
                if (!IsRegularPath(input)) return new ParseResult();

                string[] pathPoints = path.Split('/');
                string[] inputPoints = input.Split('/');

                // incompatible paths
                if (pathPoints.Length != inputPoints.Length) return new ParseResult();

                // is temp!
                List<KeyValuePair<string, string>> paramsList = new List<KeyValuePair<string, string>>();

                // processing
                for (int i = 0; i < pathPoints.Length; i++)
                {
                    string pathValue = pathPoints[i];
                    string inputValue = inputPoints[i];

                    // skin isn't custom path 
                    if (pathValue == inputValue) continue;

                    // check if is custom path
                    if (!ValidateParamFieldRegex.IsMatch(pathValue))
                    {
                        // isn't custom path and it mean that the paths is incompatible
                        return new ParseResult();
                    }

                    // remove especial characters
                    var regularParamName = pathValue.ToList();
                    regularParamName.RemoveAt(0); // remove "{"
                    regularParamName.RemoveAt(regularParamName.Count - 1); // remove "}"

                    // the custom path is find on this path
                    // left is key, right is value
                    paramsList.Add(new KeyValuePair<string, string>(new string(regularParamName.ToArray()),
                        inputValue));
                }

                // empty params
                if (paramsList.Count <= 0) return new ParseResult();

                return new ParseResult(valid: true, @params: paramsList.ToArray());
            }
        }
    }
}