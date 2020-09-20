using System.Collections.Generic;

namespace Neo.IO
{
    internal class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
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

        public unsafe int GetHashCode(byte[] obj)
        {
            if (obj is null) return -1;

            unchecked
            {
                int hash = 17;
                int len = obj.Length;
                fixed (byte* xp = obj)
                {
                    int* xlp = (int*)xp;
                    for (; len >= 4; len -= 4)
                    {
                        hash = hash * 31 + (*xlp);
                        xlp++;
                    }
                    byte* xbp = (byte*)xlp;
                    for (; len > 0; len--)
                    {
                        hash = hash * 31 + (*xbp);
                        xbp++;
                    }
                }

                return hash;
            }
        }
    }
}
