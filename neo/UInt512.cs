using System;
using System.Globalization;
using System.Linq;

namespace Neo
{
    /// <summary>
    /// This class stores a 512 bit unsigned int, represented as a 64-byte little-endian byte array
    /// </summary>
    public class UInt512 : UIntBase, IComparable<UInt512>, IEquatable<UInt512>
    {
        public static readonly UInt512 Zero = new UInt512();


        /// <summary>
        /// The empty constructor stores a null byte array
        /// </summary>
        public UInt512()
            : this(null)
        {
        }

        /// <summary>
        /// The byte[] constructor invokes base class UIntBase constructor for 64 bytes
        /// </summary>
        public UInt512(byte[] value)
            : base(64, value)
        {
        }

        /// <summary>
        /// Method CompareTo returns 1 if this UInt512 is bigger than other UInt512; -1 if it's smaller; 0 if it's equals
        /// Example: assume this is 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4,
        /// this.CompareTo(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a302ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) returns 1
        /// </summary>
        public int CompareTo(UInt512 other)
        {
            byte[] x = ToArray();
            byte[] y = other.ToArray();
            for (int i = x.Length - 1; i >= 0; i--)
            {
                if (x[i] > y[i])
                    return 1;
                if (x[i] < y[i])
                    return -1;
            }
            return 0;
        }

        /// <summary>
        /// Method Equals returns true if objects are equal, false otherwise
        /// </summary>
        bool IEquatable<UInt512>.Equals(UInt512 other)
        {
            return Equals(other);
        }

        /// <summary>
        /// Method Parse receives a big-endian hex string and stores as a UInt512 little-endian 64-bytes array
        /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01a400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01")
        /// should create UInt512 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static new UInt512 Parse(string s)
        {
            if (s == null)
                throw new ArgumentNullException();
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 128)
                throw new FormatException();
            return new UInt512(s.HexToBytes().Reverse().ToArray());
        }

        /// <summary>
        /// Method TryParse tries to parse a big-endian hex string and store it as a UInt512 little-endian 64-bytes array
        /// Example: TryParse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01a400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01", result)
        /// should create result UInt512 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static bool TryParse(string s, out UInt512 result)
        {
            if (s == null)
            {
                result = null;
                return false;
            }
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 128)
            {
                result = null;
                return false;
            }
            byte[] data = new byte[64];
            for (int i = 0; i < 64; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
                {
                    result = null;
                    return false;
                }
            result = new UInt512(data.Reverse().ToArray());
            return true;
        }

        /// <summary>
        /// Operator > returns true if left UInt512 is bigger than right UInt512
        /// Example: UInt512(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4)
        ///             > UInt512(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a302ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
        /// </summary>
        public static bool operator >(UInt512 left, UInt512 right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Operator >= returns true if left UInt512 is bigger or equals to right UInt512
        /// Example: UInt512(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4)
        ///             >= UInt512(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a302ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
        /// </summary>
        public static bool operator >=(UInt512 left, UInt512 right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Operator < returns true if left UInt512 is less than right UInt512
        /// Example: UInt512(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a302ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3)
        ///            < UInt512(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
        /// </summary>
        public static bool operator <(UInt512 left, UInt512 right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Operator <= returns true if left UInt512 is less or equals to right UInt512
        /// Example: UInt512(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a302ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3)
        ///             <= UInt512(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a401ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
        /// </summary>
        public static bool operator <=(UInt512 left, UInt512 right)
        {
            return left.CompareTo(right) <= 0;
        }
    }
}
