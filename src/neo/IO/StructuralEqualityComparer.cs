using System.Collections;
using System.Collections.Generic;

namespace Neo.IO
{
    internal class StructuralEqualityComparer<T> : IEqualityComparer<byte[]>
    {
        public static readonly StructuralEqualityComparer<T> Default = new StructuralEqualityComparer<T>();

        public bool Equals(byte[] x, byte[] y)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
        }

        public int GetHashCode(byte[] obj)
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }
}
