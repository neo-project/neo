using System;

namespace Neo.Trie.MPT
{
    internal static class Helper
    {
        public static byte[] CommonPrefix(this byte[] a, byte[] b)
        {
            if (a is null || b is null) return Array.Empty<byte>();
            var minLen = a.Length <= b.Length ? a.Length : b.Length;
            int i = 0;
            if (a.Length != 0 && b.Length != 0)
            {
                for (i = 0; i < minLen; i++)
                {
                    if (a[i] != b[i]) break;
                }
            }
            return a[..i];
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
