using System;

namespace Neo.Trie.MPT
{
    public static class Helper
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
            var prefix = new byte[]{};
            var minLen = a.Length <= b.Length ? a.Length : b.Length;

            if (a.Length == 0 || b.Length == 0 || a[0] != b[0]) return prefix;

            for (int i = 0; i < minLen; i++)
            {
                if (a[i] != b[i]) break;
                prefix = prefix.Add(a[i]);
            }
            return prefix;
        }

        public static bool Equal(this byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public static byte[] Skip(this byte[] a, int count)
        {
            var result = new byte[]{};
            var len = a.Length - count;
            if (0 < len) {
                result = new byte[len];
                Array.Copy(a, count, result, 0, len);
            }
            return result;
        }

        public static byte[] Add(this byte[] a, byte b)
        {
            var result = new byte[a.Length + 1];
            a.CopyTo(result, 0);
            result[a.Length] = b;
            return result;
        }
    }
}