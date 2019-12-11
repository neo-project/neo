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
                    throw new FormatException($"Invalid Base58 character '{input[i]}' at position {i}");
                bi = bi * Alphabet.Length + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            int leadingZeroCount = input.TakeWhile(c => c == Alphabet[0]).Count();
            var leadingZeros = new byte[leadingZeroCount];
            var bytesWithoutLeadingZeros = bi.ToByteArray()
                .Reverse()// to big endian
                .SkipWhile(b => b == 0);//strip sign byte
            return leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
        }

        public static string Encode(byte[] input)
        {
            // Decode byte[] to BigInteger
            BigInteger value = new BigInteger(new byte[1].Concat(input).Reverse().ToArray());

            // Encode BigInteger to Base58 string
            var sb = new StringBuilder();

            while (value > 0)
            {
                value = BigInteger.DivRem(value, Alphabet.Length, out var remainder);
                sb.Insert(0, Alphabet[(int)remainder]);
            }

            // Append `1` for each leading 0 byte
            for (int i = 0; i < input.Length && input[i] == 0; i++)
            {
                sb.Insert(0, Alphabet[0]);
            }
            return sb.ToString();
        }
    }
}
