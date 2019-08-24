using System;
using System.Collections.Generic;

namespace Neo.IO
{
    internal class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Default = new ByteArrayComparer();

        public int Compare(byte[] x, byte[] y)
        {
            int length = Math.Min(x.Length, y.Length);
            for (int i = 0; i < length; i++)
            {
                int r = x[i].CompareTo(y[i]);
                if (r != 0) return r;
            }
            return x.Length.CompareTo(y.Length);
        }
    }
}
