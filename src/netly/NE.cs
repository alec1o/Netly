using System;
using System.Text;

namespace Netly
{
    /// <summary>
    /// NE (Netly Encoding)
    /// </summary>
    public static class NE
    {
        /// <summary>
        /// Is the default (generic) encoder used when encoding is not specified<br/>
        /// Default value is: UTF8
        /// </summary>
        public static Encoding DefaultEncoding { get; set; } = Encoding.UTF8;


        /// <summary>
        /// Convert string to bytes (using NE.Default Mode)
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public static byte[] GetBytes(string value)
        {
            return GetBytes(value, DefaultEncoding);
        }

        /// <summary>
        /// Convert bytes to string (using NE.Default Mode)
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public static string GetString(byte[] value)
        {
            return GetString(value, DefaultEncoding);
        }

        /// <summary>
        /// Convert bytes to string
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encoding">Encoding method</param>
        /// <returns></returns>
        public static string GetString(byte[] value, Encoding encoding)
        {
            return encoding.GetString(value);
        }

        /// <summary>
        /// Convert value to bytes
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encoding">Encoding method</param>
        /// <returns></returns>
        public static byte[] GetBytes(string value, Encoding encoding)
        {
            return encoding.GetBytes(value);
        }

        /// <summary>
        /// Detect and return encoding
        /// </summary>
        /// <param name="data">Data - string</param>
        /// <returns>Detected encoder</returns>
        public static Encoding GetEncoding(string data)
        {
            return Encoding.GetEncoding(data);
        }
    }
}