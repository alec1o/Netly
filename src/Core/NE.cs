using System.Text;

namespace Netly.Core
{
    /// <summary>
    /// Netly: NE (Netly Encode). It is a module that redraws the (System.Text.Encoding) syntax
    /// </summary>
    public static class NE
    {
        /// <summary>
        /// Are the supported encoders method
        /// </summary>
        public enum Mode { ASCII = 0, UTF8 = 1, UNICODE = 2 }

        /// <summary>
        /// Is the default (generic) encoder used when encoding is not specified
        /// </summary>
        public static Mode Default { get; set; } = Mode.UTF8;

        /// <summary>
        /// Convert value to string
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return value converted to string</returns>
        public static string GetString(byte[] value)
        {
            return GetString(value, Default);
        }

        /// <summary>
        /// Convert value to string
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encode">Encode method</param>
        /// <returns>Return value converted to string</returns>
        public static string GetString(byte[] value, Mode encode)
        {
            switch (encode)
            {
                case Mode.UTF8:
                    {
                        return Encoding.UTF8.GetString(value);
                    }
                case Mode.ASCII:
                    {
                        return Encoding.ASCII.GetString(value);
                    }
                case Mode.UNICODE:
                    {
                        return Encoding.Unicode.GetString(value);
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        /// <summary>
        /// Convert value to bytes
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return value converted to bytes</returns>
        public static byte[] GetBytes(string value)
        {
            return GetBytes(value, Default);
        }

        /// <summary>
        /// Convert value to bytes
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="encode">Encode method</param>
        /// <returns>Return value converted to bytes</returns>
        public static byte[] GetBytes(string value, Mode encode)
        {
            switch (encode)
            {
                case Mode.UTF8:
                    {
                        return Encoding.UTF8.GetBytes(value);
                    }
                case Mode.ASCII:
                    {
                        return Encoding.ASCII.GetBytes(value);
                    }
                case Mode.UNICODE:
                    {
                        return Encoding.Unicode.GetBytes(value);
                    }
                default:
                    {
                        return new byte[0];
                    }
            }
        }
    }
}
