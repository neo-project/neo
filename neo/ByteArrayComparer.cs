using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Cryptography;

namespace Neo
{
    /// <inheritdoc />
    /// <summary>
    /// Byte array comparer, useful to have a byte array on a set or as key of a dictionary.
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        /// <inheritdoc />
        public bool Equals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return left == null && right == null;
            }

            return left.SequenceEqual(right);
        }

        /// <inheritdoc />
        public int GetHashCode(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return (int) key.Murmur32(0);
        }
    }
}