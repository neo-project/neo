using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.Cryptography
{
    public static class Base58
    {
        public const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static byte[] Decode(string input)
        {
            // Decode Base58 string to BigInteger 
            var bi = BigInteger.Zero;
            for (int i = 0; i < input.Length; i++)
            {
                int digit = Alphabet.IndexOf(input[i]);
                if (digit < 0)
                    throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", input[i], i));
                bi = bi * 58 + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            int leadingZeroCount = input.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros =
                bi.ToByteArray()
                .Reverse()// to big endian
                .SkipWhile(b => b == 0);//strip sign byte
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
            return result;
        }

        public static string Encode(byte[] input)
        {
            // Decode byte[] to BigInteger
            var intData = BigInteger.Zero;
            for (int i = 0; i < input.Length; i++)
            {
                intData = intData * 256 + input[i];
            }

            // Encode BigInteger to Base58 string
            var sb = new StringBuilder();

            while (intData > 0)
            {
                int remainder = (int)(intData % 58);
                intData /= 58;
                sb.Insert(0, Alphabet[remainder]);
            }

            // Append `1` for each leading 0 byte
            for (int i = 0; i < input.Length && input[i] == 0; i++)
            {
                sb.Insert(0, "1");
            }
            return sb.ToString();
        }
    }
}
