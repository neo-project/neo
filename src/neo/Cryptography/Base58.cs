using System;
using System.Linq;
using System.Numerics;
using System.Text;
using static Neo.Helper;

namespace Neo.Cryptography
{
    public static class Base58
    {
        public const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static byte[] Base58CheckDecode(this string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            byte[] buffer = Decode(input);
            if (buffer.Length < 4) throw new FormatException();
            byte[] checksum = buffer.Sha256(0, buffer.Length - 4).Sha256();
            if (!buffer.AsSpan(^4).SequenceEqual(checksum.AsSpan(..4)))
                throw new FormatException();
            var ret = buffer[..^4];
            Array.Clear(buffer, 0, buffer.Length);
            return ret;
        }

        public static string Base58CheckEncode(this ReadOnlySpan<byte> data)
        {
            byte[] checksum = data.Sha256().Sha256();
            Span<byte> buffer = stackalloc byte[data.Length + 4];
            data.CopyTo(buffer);
            checksum.AsSpan(..4).CopyTo(buffer[data.Length..]);
            var ret = Encode(buffer);
            buffer.Clear();
            return ret;
        }

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
            if (bi.IsZero) return leadingZeros;
            var bytesWithoutLeadingZeros = bi.ToByteArray(isUnsigned: true, isBigEndian: true);
            return Concat(leadingZeros, bytesWithoutLeadingZeros);
        }

        public static string Encode(ReadOnlySpan<byte> input)
        {
            // Decode byte[] to BigInteger
            BigInteger value = new BigInteger(input, isUnsigned: true, isBigEndian: true);

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
