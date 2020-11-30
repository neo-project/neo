using System.Collections.Generic;

namespace Neo.IO
{
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Default = new ByteArrayEqualityComparer();

        public unsafe bool Equals(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            int len = x.Length;
            if (len != y.Length) return false;
            if (len == 0) return true;
            fixed (byte* xp = x, yp = y)
            {
                long* xlp = (long*)xp, ylp = (long*)yp;
                for (; len >= 8; len -= 8)
                {
                    if (*xlp != *ylp) return false;
                    xlp++;
                    ylp++;
                }
                byte* xbp = (byte*)xlp, ybp = (byte*)ylp;
                for (; len > 0; len--)
                {
                    if (*xbp != *ybp) return false;
                    xbp++;
                    ybp++;
                }
            }
            return true;
        }

        public int GetHashCode(byte[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (byte element in obj)
                    hash = hash * 31 + element;
                return hash;
            }
        }
    }
}
