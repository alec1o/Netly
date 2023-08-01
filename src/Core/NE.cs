using System;
using System.Text;

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
        public enum Mode
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
        public static Mode Default { get; set; } = Mode.UTF8;


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
        /// <param name="encode">Encoding protocol</param>
        /// <returns></returns>
        public static string GetString(byte[] value, Mode encode)
        {
            switch (encode)
            {
                case Mode.ASCII: return Encoding.ASCII.GetString(value);
                case Mode.UTF7: return Encoding.UTF7.GetString(value);
                case Mode.UTF8: return Encoding.UTF8.GetString(value);
                case Mode.UTF16: return Encoding.Unicode.GetString(value);
                case Mode.UTF32: return Encoding.UTF32.GetString(value);
                case Mode.UNICODE: return Encoding.Unicode.GetString(value);
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Convert value to bytes
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encode">Encoding protocol</param>
        /// <returns></returns>
        public static byte[] GetBytes(string value, Mode encode)
        {
            switch (encode)
            {
                case Mode.ASCII: return Encoding.ASCII.GetBytes(value);
                case Mode.UTF7: return Encoding.UTF7.GetBytes(value);
                case Mode.UTF8: return Encoding.UTF8.GetBytes(value);
                case Mode.UTF16: return Encoding.Unicode.GetBytes(value);
                case Mode.UTF32: return Encoding.UTF32.GetBytes(value);
                case Mode.UNICODE: return Encoding.Unicode.GetBytes(value);
                default: return Array.Empty<byte>();
            }
        }
    }
}
