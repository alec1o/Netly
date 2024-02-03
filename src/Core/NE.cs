using System;

namespace Netly.Core
{
    /// <summary>
    /// NE (Netly Encoding)
    /// </summary>
    public static class NE
    {
        /// <summary>
        /// Are the supported encoders method
        /// </summary>
        public enum Encoding
        {
            /// <summary>
            /// ASCII
            /// </summary>
            ASCII = 0,

            /// <summary>
            /// UTF-7
            /// </summary>
            UTF7 = 1,

            /// <summary>
            /// UTF-8
            /// </summary>
            UTF8 = 2,

            /// <summary>
            /// UTF-16
            /// </summary>
            UTF16 = 3,

            /// <summary>
            /// UTF-32
            /// </summary>
            UTF32 = 4,

            /// <summary>
            /// UNICODE is UTF-16
            /// </summary>
            UNICODE = 5,
        }

        /// <summary>
        /// Is the default (generic) encoder used when encoding is not specified<br/>
        /// Default value is: UTF8
        /// </summary>
        public static Encoding Default { get; set; } = Encoding.UTF8;


        /// <summary>
        /// Convert string to bytes (using NE.Default Mode)
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public static byte[] GetBytes(string value)
        {
            return GetBytes(value, Default);
        }

        /// <summary>
        /// Convert bytes to string (using NE.Default Mode)
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public static string GetString(byte[] value)
        {
            return GetString(value, Default);
        }

        /// <summary>
        /// Convert bytes to string
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encoding">Encoding protocol</param>
        /// <returns></returns>
        public static string GetString(byte[] value, Encoding encoding)
        {
            switch (encoding)
            {
                case Encoding.ASCII: return System.Text.Encoding.ASCII.GetString(value);
                case Encoding.UTF7: return System.Text.Encoding.UTF7.GetString(value);
                case Encoding.UTF8: return System.Text.Encoding.UTF8.GetString(value);
                case Encoding.UTF16: return System.Text.Encoding.Unicode.GetString(value);
                case Encoding.UTF32: return System.Text.Encoding.UTF32.GetString(value);
                case Encoding.UNICODE: return System.Text.Encoding.Unicode.GetString(value);
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Convert value to bytes
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encoding">Encoding protocol</param>
        /// <returns></returns>
        public static byte[] GetBytes(string value, Encoding encoding)
        {
            switch (encoding)
            {
                case Encoding.ASCII: return System.Text.Encoding.ASCII.GetBytes(value);
                case Encoding.UTF7: return System.Text.Encoding.UTF7.GetBytes(value);
                case Encoding.UTF8: return System.Text.Encoding.UTF8.GetBytes(value);
                case Encoding.UTF16: return System.Text.Encoding.Unicode.GetBytes(value);
                case Encoding.UTF32: return System.Text.Encoding.UTF32.GetBytes(value);
                case Encoding.UNICODE: return System.Text.Encoding.Unicode.GetBytes(value);
                default: return Array.Empty<byte>();
            }
        }

        public static System.Text.Encoding GetNativeEncodingFromProtocol(Encoding encodingEncoding)
        {
            switch (encodingEncoding)
            {
                case Encoding.ASCII: return System.Text.Encoding.ASCII;
                case Encoding.UTF7: return System.Text.Encoding.UTF7;
                case Encoding.UTF8: return System.Text.Encoding.UTF8;
                case Encoding.UTF16: return System.Text.Encoding.Unicode;
                case Encoding.UTF32: return System.Text.Encoding.UTF32;
                case Encoding.UNICODE: return System.Text.Encoding.Unicode;
                default: throw new NotImplementedException(encodingEncoding.ToString());
            }
        }

        public static Encoding GetProtocolFromNativeEncoding(System.Text.Encoding encoding)
        {
            switch (encoding.WebName.Trim().ToUpper())
            {
                case "ASCII": return Encoding.ASCII;
                case "USASCII": return Encoding.ASCII;
                case "US-ASCII": return Encoding.ASCII;
                
                case "UNICODE": return Encoding.UNICODE;

                case "UTF7": return Encoding.UTF7;
                case "UTF-7": return Encoding.UTF7;

                case "UTF8": return Encoding.UTF8;
                case "UTF-8": return Encoding.UTF8;

                case "UTF16": return Encoding.UTF16;
                case "UTF-16": return Encoding.UTF16;

                case "UTF32": return Encoding.UTF32;
                case "UTF-32": return Encoding.UTF32;
                
                default: throw new NotImplementedException(encoding.ToString());
            }
        }
    }
}