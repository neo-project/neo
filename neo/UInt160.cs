using System;
using System.Globalization;
using System.Linq;

namespace Neo
{
    /// <summary>
    /// This class stores a 160 bit unsigned int, represented as a 20-byte little-endian byte array
    /// </summary>
    public class UInt160 : UIntBase, IComparable<UInt160>, IEquatable<UInt160>
    {
        public static readonly UInt160 Zero = new UInt160();

        /// <summary>
        /// The empty constructor stores a null byte array
        /// </summary>
        public UInt160()
            : this(null)
        {
        }

        /// <summary>
        /// The byte[] constructor invokes base class UIntBase constructor for 20 bytes
        /// </summary>
        public UInt160(byte[] value)
            : base(20, value)
        {
        }

        /// <summary>
        /// Method CompareTo returns 1 if this UInt160 is bigger than other UInt160; -1 if it's smaller; 0 if it's equals
        /// Example: assume this is 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, this.CompareTo(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) returns 1
        /// </summary>
        public int CompareTo(UInt160 other)
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
        bool IEquatable<UInt160>.Equals(UInt160 other)
        {
            return Equals(other);
        }

        /// <summary>
        /// Method Parse receives a big-endian hex string and stores as a UInt160 little-endian 20-bytes array
        /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") should create UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static new UInt160 Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException();
            if (value.StartsWith("0x"))
                value = value.Substring(2);
            if (value.Length != 40)
                throw new FormatException();
            return new UInt160(value.HexToBytes().Reverse().ToArray());
        }

        /// <summary>
        /// Method TryParse tries to parse a big-endian hex string and store it as a UInt160 little-endian 20-bytes array
        /// Example: TryParse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01", result) should create result UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static bool TryParse(string s, out UInt160 result)
        {
            if (s == null)
            {
                result = null;
                return false;
            }
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 40)
            {
                result = null;
                return false;
            }
            byte[] data = new byte[20];
            for (int i = 0; i < 20; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
                {
                    result = null;
                    return false;
                }
            result = new UInt160(data.Reverse().ToArray());
            return true;
        }

        /// <summary>
        /// Operator > returns true if left UInt160 is bigger than right UInt160
        /// Example: UInt160(01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) > UInt160 (02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
        /// </summary>
        public static bool operator >(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Operator > returns true if left UInt160 is bigger or equals to right UInt160
        /// Example: UInt160(01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) >= UInt160 (02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
        /// </summary>
        public static bool operator >=(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Operator > returns true if left UInt160 is less than right UInt160
        /// Example: UInt160(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) < UInt160 (01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
        /// </summary>
        public static bool operator <(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Operator > returns true if left UInt160 is less or equals to right UInt160
        /// Example: UInt160(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) < UInt160 (01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
        /// </summary>
        public static bool operator <=(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) <= 0;
        }
    }
}
