using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace AntShares
{
    internal static class BigIntegerExtensions
    {
        public static BigInteger SetBit(this BigInteger n, int bit)
        {
            return n |= BigInteger.One << bit;
        }

        public static bool TestBit(this BigInteger n, int bit)
        {
            return (n & (BigInteger.One << bit)) != 0;
        }

        public static int GetBitLength(this BigInteger n)
        {
            var remainder = n;
            int bits = 0;
            while (remainder > 0)
            {
                remainder = remainder >> 1;
                bits++;
            }

            return bits;
        }

        public static string ToPolynomialString(this BigInteger n)
        {
            var sb = new StringBuilder();
            for (int i = n.GetBitLength(); i >= 0; i--)
            {
                if (n.TestBit(i))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" + ");
                    }

                    sb.Append((i > 0) ? "x" : "1");

                    if (i > 1)
                    {
                        sb.Append("^");
                        sb.Append(i);
                    }
                }
            }

            if (sb.Length == 0)
            {
                sb.Append("0");
            }

            return sb.ToString();
        }

        public static byte[] ToUnsignedLittleEndianBytes(this BigInteger n)
        {
            var byteArray = n.ToByteArray();
            if ((byteArray.Length > 1) && (byteArray[byteArray.Length - 1] == 0x00))
            {
                var byteArrayMissingEnd = new byte[byteArray.Length - 1];
                Array.Copy(byteArray, byteArrayMissingEnd, byteArrayMissingEnd.Length);
                return byteArrayMissingEnd;
            }
            return byteArray;
        }

        public static byte[] ToUnsignedBigEndianBytes(this BigInteger n)
        {
            var bytes = n.ToUnsignedLittleEndianBytes();
            Array.Reverse(bytes);
            return bytes;
        }

        public static BigInteger ToBigIntegerFromLittleEndianUnsignedBytes(this byte[] bytes)
        {
            return new BigInteger(bytes.Concat(new byte[1]).ToArray());
        }

        public static BigInteger ToBigIntegerFromBigEndianUnsignedBytes(this byte[] bytes)
        {
            byte[] littleEndianBytes = bytes.Reverse().ToArray();
            return littleEndianBytes.ToBigIntegerFromLittleEndianUnsignedBytes();
        }
    }
}
