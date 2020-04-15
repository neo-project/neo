using System;

namespace Neo.Trie.MPT
{
    internal static class Helper
    {
        public static byte[] Concat(this byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            a.CopyTo(result, 0);
            b.CopyTo(result, a.Length);
            return result;
        }

        public static byte[] CommonPrefix(this byte[] a, byte[] b)
        {
            if (a is null || b is null) return Array.Empty<byte>();
            var minLen = a.Length <= b.Length ? a.Length : b.Length;
            int i = 0;
            for (i = 0; i < minLen; i++)
            {
                if (a[i] != b[i]) break;
            }
            var prefix = new byte[i];
            Array.Copy(a, prefix, i);
            return prefix;
        }

        public static byte[] Skip(this byte[] a, int count)
        {
            var result = Array.Empty<byte>();
            var len = a.Length - count;
            if (0 < len)
            {
                result = new byte[len];
                Array.Copy(a, count, result, 0, len);
            }
            return result;
        }

        public static byte[] ToNibbles(this byte[] path)
        {
            if (path is null) return Array.Empty<byte>();
            var result = new byte[path.Length * 2];
            for (int i = 0; i < path.Length; i++)
            {
                result[i * 2] = (byte)(path[i] >> 4);
                result[i * 2 + 1] = (byte)(path[i] & 0x0F);
            }
            return result;
        }
    }
}
