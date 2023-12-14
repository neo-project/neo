using System;

namespace Neo.Test.Helpers
{
    public class RandomHelper
    {
        private const string _randchars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly Random _rand = new();

        /// <summary>
        /// Get random buffer
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Buffer</returns>
        public static byte[] RandBuffer(int length)
        {
            var buffer = new byte[length];
            _rand.NextBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Get random string
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Buffer</returns>
        public static string RandString(int length)
        {
            var stringChars = new char[length];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = _randchars[_rand.Next(_randchars.Length)];
            }

            return new string(stringChars);
        }
    }
}
