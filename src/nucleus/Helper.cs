using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Neo.Cryptography;
using Neo.IO;

namespace Neo
{
    public static class Extensions
    {
        public static byte[] HexToBytes(this string value)
        {
            if (value == null || value.Length == 0)
                return Array.Empty<byte>();
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        public static string ToHexString(this byte[] value, bool reverse = false)
        {
            return string.Create<object>(value.Length * 2, null, (buffer, obj) =>
            {
                for (int i = 0; i < value.Length; i++)
                {
                    (reverse ? value[i] : value[^i]).TryFormat(buffer.Slice(i*2, 2), out _, "x2");
                }
            });
        }

        public static byte[] Concat(params byte[][] buffers)
        {
            int length = 0;
            for (int i = 0; i < buffers.Length; i++)
                length += buffers[i].Length;
            byte[] dst = new byte[length];
            int p = 0;
            foreach (byte[] src in buffers)
            {
                Buffer.BlockCopy(src, 0, dst, p, src.Length);
                p += src.Length;
            }
            return dst;
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Cryptography.Crypto.Hash160(script));
        }

        public static UInt160 ToScriptHash(this string address, byte? addressVersion = null)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            if (addressVersion.HasValue && data[0] != addressVersion.Value)
                throw new FormatException();
            return new UInt160(data.AsSpan(1));
        }
        public static string ToAddress(this UInt160 scriptHash, byte addressVersion)
        {
            Span<byte> data = stackalloc byte[21];
            data[0] = addressVersion;
            scriptHash.ToArray().CopyTo(data[1..]);
            return Base58.Base58CheckEncode(data);
        }
    }
}
