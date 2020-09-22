using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Neo.IO
{
    public static class Helper
    {
        public static string ToHexString(this byte[] value, bool reverse = false)
        {
            return ToHexString(value.AsSpan(), reverse);
        }

        public static string ToHexString(this ReadOnlySpan<byte> value, bool reverse = false)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
                sb.AppendFormat("{0:x2}", value[reverse ? value.Length - i - 1 : i]);
            return sb.ToString();
        }

        public static byte[] HexToBytes(this string value)
        {
            return HexToBytes(value.AsSpan());
        }

        public static byte[] HexToBytes(this ReadOnlySpan<char> value)
        {
            if (value == null || value.Length == 0)
                return Array.Empty<byte>();
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Slice(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        public static byte[] ToArray(this Neo.IO.ISerializable value)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms, Encoding.StrictUTF8);
            value.Serialize(writer);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
