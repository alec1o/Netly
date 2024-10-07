namespace Netly
{
    public static partial class HTTP
    {
        /// <summary>
        /// HTTP Enctype Header
        /// </summary>
        public enum Enctype : sbyte
        {
            /// <summary>
            /// Used when the enctype is not recognized or unsupported.
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// No encoding is specified.
            /// </summary>
            None = 0,

            /// <summary>
            /// Default for form submissions. Sends data as `application/x-www-form-urlencoded`.
            /// (Parser Supported)
            /// </summary>
            UrlEncoded = 1,

            /// <summary>
            /// Sends data as `multipart/form-data`. Used for file uploads.
            /// (Parser Supported)
            /// </summary>
            Multipart = 2,

            /// <summary>
            /// Sends data as plain text using `text/plain`.
            /// </summary>
            PlainText = 3,

            /// <summary>
            /// Sends data as JSON using `application/json`. Common in RESTful APIs.
            /// (Parser Supported)
            /// </summary>
            Json = 4,

            /// <summary>
            /// Sends data as XML using `application/xml`. 
            /// </summary>
            Xml = 5,

            /// <summary>
            /// Sends binary data using `application/octet-stream`.
            /// </summary>
            OctetStream = 6,

            /// <summary>
            /// Sends CSV (Comma Separated Values) using `text/csv`.
            /// </summary>
            Csv = 7,

            /// <summary>
            /// Sends HTML content using `text/html`.
            /// </summary>
            Html = 8,

            /// <summary>
            /// Sends data as `application/pdf`.
            /// </summary>
            Pdf = 9,

            /// <summary>
            /// Sends JavaScript code using `application/javascript`.
            /// </summary>
            Javascript = 10,

            /// <summary>
            /// Sends YAML data using `application/x-yaml`. 
            /// (Parser Supported)
            /// </summary>
            Yaml = 11,

            /// <summary>
            /// Sends GraphQL queries using `application/graphql`.
            /// </summary>
            GraphQL = 12,

            /// <summary>
            /// Sends SOAP XML data using `application/soap+xml`.
            /// </summary>
            SoapXml = 13,

            /// <summary>
            /// Sends ZIP files using `application/zip`.
            /// </summary>
            Zip = 14,

            /// <summary>
            /// Sends form data encoded as `multipart/related`. Typically used in APIs or email-related transfers, where multiple parts of a message are related but differ in content type. Requires combining various parts.
            /// (Parser Supported)
            /// </summary>
            MultipartRelated = 15,

            /// <summary>
            /// Sends WebAssembly files using `application/wasm`.
            /// </summary>
            WebAssembly = 16,

            /// <summary>
            /// Sends Markdown text using `text/markdown`.
            /// </summary>
            Markdown = 17
        }
    }
}
