using System;
using System.Collections.Generic;

namespace Neo.IO
{
    internal class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Default = new ByteArrayComparer(1);
        public static readonly ByteArrayComparer Reverse = new ByteArrayComparer(-1);

        private readonly int direction;

        private ByteArrayComparer(int direction)
        {
            this.direction = direction;
        }

        public int Compare(byte[] x, byte[] y)
        {
            int r;
            int length = Math.Min(x.Length, y.Length);
            for (int i = 0; i < length; i++)
            {
                r = x[i].CompareTo(y[i]);
                if (direction == -1) r = -r;
                if (r != 0) return r;
            }
            r = x.Length.CompareTo(y.Length);
            if (direction == -1) r = -r;
            return r;
        }
    }
}
