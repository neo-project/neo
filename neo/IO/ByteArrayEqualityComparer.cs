using System;
using System.Collections.Generic;

namespace Neo.IO
{
    internal class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();

        public bool Equals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public int GetHashCode(byte[] a)
        {
            return a.ToInt32(0);
        }
    }
}
