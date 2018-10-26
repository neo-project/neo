using Neo.IO;
using System;
using System.IO;
using System.Linq;

namespace Neo
{
    
    /// <summary>
    /// Base class for little-endian unsigned integers. Two classes inherit from this: UInt160 and UInt256.
    /// Only basic comparison/serialization are proposed for these classes. For arithmetic purposes, use BigInteger class.
    /// </summary>
    public abstract class UIntBase : IEquatable<UIntBase>, ISerializable
    {
        /// <summary>
        /// Storing unsigned int in a little-endian byte array.
        /// </summary>
        private byte[] data_bytes;

        /// <summary>
        /// Number of bytes of the unsigned int.
        /// Currently, inherited classes use 20-bytes (UInt160) or 32-bytes (UInt256)
        /// </summary>
        public int Size => data_bytes.Length;

        /// <summary>
        /// Base constructor receives the intended number of bytes and a byte array. 
        /// If byte array is null, it's automatically initialized with given size.
        /// </summary>
        protected UIntBase(int bytes, byte[] value)
        {
            if (value == null)
            {
                this.data_bytes = new byte[bytes];
                return;
            }
            if (value.Length != bytes)
                throw new ArgumentException();
            this.data_bytes = value;
        }

        /// <summary>
        /// Deserialize function reads the expected size in bytes from the given BinaryReader and stores in data_bytes array.
        /// </summary>
        void ISerializable.Deserialize(BinaryReader reader)
        {
            reader.Read(data_bytes, 0, data_bytes.Length);
        }

        /// <summary>
        /// Method Equals returns true if objects are equal, false otherwise
        /// If null is passed as parameter, this method returns false. If it's a self-reference, it returns true.
        /// </summary>
        public bool Equals(UIntBase other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (data_bytes.Length != other.data_bytes.Length)
                return false;
            return data_bytes.SequenceEqual(other.data_bytes);
        }

        /// <summary>
        /// Method Equals returns true if objects are equal, false otherwise
        /// If null is passed as parameter or if it's not a UIntBase object, this method returns false.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
            if (!(obj is UIntBase))
                return false;
            return this.Equals((UIntBase)obj);
        }

        /// <summary>
        /// Method GetHashCode returns a 32-bit int representing a hash code, composed of the first 4 bytes.
        /// </summary>
        public override int GetHashCode()
        {
            return data_bytes.ToInt32(0);
        }

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

        /// <summary>
        /// Method Serialize writes the data_bytes array into a BinaryWriter object
        /// </summary>
        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(data_bytes);
        }

        /// <summary>
        /// Method ToArray() returns the byte array data_bytes, which stores the little-endian unsigned int
        /// </summary>
        public byte[] ToArray()
        {
            return data_bytes;
        }

        /// <summary>
        /// Method ToString returns a big-endian string starting by "0x" representing the little-endian unsigned int
        /// Example: if this is storing 20-bytes 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, ToString() should return "0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"
        /// </summary>
        public override string ToString()
        {
            return "0x" + data_bytes.Reverse().ToHexString();
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

        /// <summary>
        /// Operator == returns true if left UIntBase is equals to right UIntBase
        /// If any parameter is null, it returns false. If both are the same object, it returns true.
        /// Example: UIntBase(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) == UIntBase(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
        /// </summary>
        public static bool operator ==(UIntBase left, UIntBase right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Operator != returns true if left UIntBase is not equals to right UIntBase
        /// Example: UIntBase(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) != UIntBase(01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
        /// </summary>
        public static bool operator !=(UIntBase left, UIntBase right)
        {
            return !(left == right);
        }
    }
}
