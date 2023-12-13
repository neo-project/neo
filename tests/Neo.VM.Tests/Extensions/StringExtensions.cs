using System;
using System.Globalization;

namespace Neo.Test.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Convert buffer to hex string
        /// </summary>
        /// <param name="data">Data</param>
        /// <returns>Return hex string</returns>
        public static string ToHexString(this byte[] data)
        {
            if (data == null) return "";

            var m = data.Length;
            if (m == 0) return "";

            var sb = new char[(m * 2) + 2];

            sb[0] = '0';
            sb[1] = 'x';

            for (int x = 0, y = 2; x < m; x++, y += 2)
            {
                var hex = data[x].ToString("x2");

                sb[y] = hex[0];
                sb[y + 1] = hex[1];
            }

            return new string(sb);
        }

        /// <summary>
        /// Convert string in Hex format to byte array
        /// </summary>
        /// <param name="value">Hexadecimal string</param>
        /// <returns>Return byte array</returns>
        public static byte[] FromHexString(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<byte>();
            if (value.StartsWith("0x"))
                value = value[2..];
            if (value.Length % 2 == 1)
                throw new FormatException();

            var result = new byte[value.Length / 2];
            for (var i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);

            return result;
        }
    }
}
