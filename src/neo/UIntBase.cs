using Neo.IO;
using System;
using System.IO;

namespace Neo
{
    /// <summary>
    /// Base class for little-endian unsigned integers. Two classes inherit from this: UInt160 and UInt256.
    /// Only basic comparison/serialization are proposed for these classes. For arithmetic purposes, use BigInteger class.
    /// </summary>
    public abstract class UIntBase : ISerializable
    {
        /// <summary>
        /// Number of bytes of the unsigned int.
        /// Currently, inherited classes use 20-bytes (UInt160) or 32-bytes (UInt256)
        /// </summary>
        public abstract int Size { get; }

        public abstract void Deserialize(BinaryReader reader);

        public abstract override bool Equals(object obj);

        /// <summary>
        /// Method GetHashCode returns a 32-bit int representing a hash code, composed of the first 4 bytes.
        /// </summary>
        public abstract override int GetHashCode();

        /// <summary>
        /// Method Parse receives a big-endian hex string and stores as a UInt160 or UInt256 little-endian byte array
        /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") should create UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static UIntBase Parse(string s)
        {
            if (s.Length == 40 || s.Length == 42)
                return UInt160.Parse(s);
            else if (s.Length == 64 || s.Length == 66)
                return UInt256.Parse(s);
            else
                throw new FormatException();
        }

        public abstract void Serialize(BinaryWriter writer);

        /// <summary>
        /// Method ToString returns a big-endian string starting by "0x" representing the little-endian unsigned int
        /// Example: if this is storing 20-bytes 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, ToString() should return "0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"
        /// </summary>
        public override string ToString()
        {
            return "0x" + this.ToArray().ToHexString(reverse: true);
        }

        /// <summary>
        /// Method TryParse tries to parse a big-endian hex string and stores it as a UInt160 or UInt256 little-endian bytes array
        /// Example: TryParse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01", result) should create result UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static bool TryParse<T>(string s, out T result) where T : UIntBase
        {
            int size;
            if (typeof(T) == typeof(UInt160))
                size = 20;
            else if (typeof(T) == typeof(UInt256))
                size = 32;
            else if (s.Length == 40 || s.Length == 42)
                size = 20;
            else if (s.Length == 64 || s.Length == 66)
                size = 32;
            else
                size = 0;
            if (size == 20)
            {
                if (UInt160.TryParse(s, out UInt160 r))
                {
                    result = (T)(UIntBase)r;
                    return true;
                }
            }
            else if (size == 32)
            {
                if (UInt256.TryParse(s, out UInt256 r))
                {
                    result = (T)(UIntBase)r;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}
