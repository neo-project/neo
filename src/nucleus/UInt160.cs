using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Neo
{
    /// <summary>
    /// This class stores a 160 bit unsigned int, represented as a 20-byte little-endian byte array
    /// It is composed by ulong(64) + ulong(64) + uint(32) = UInt160(160)
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public class UInt160 : IComparable<UInt160>, IEquatable<UInt160>
    {
        public const int Length = 20;
        public static readonly UInt160 Zero = new UInt160();

        [FieldOffset(0)] private ulong value1;
        [FieldOffset(8)] private ulong value2;
        [FieldOffset(16)] private uint value3;

        public int Size => Length;

        public UInt160()
        {
        }

        public unsafe UInt160(ReadOnlySpan<byte> value)
        {
            fixed (ulong* p = &value1)
            {
                Span<byte> dst = new Span<byte>(p, Length);
                value[..Length].CopyTo(dst);
            }
        }

        /// <summary>
        /// Method CompareTo returns 1 if this UInt160 is bigger than other UInt160; -1 if it's smaller; 0 if it's equals
        /// Example: assume this is 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, this.CompareTo(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) returns 1
        /// </summary>
        public int CompareTo(UInt160 other)
        {
            int result = value3.CompareTo(other.value3);
            if (result != 0) return result;
            result = value2.CompareTo(other.value2);
            if (result != 0) return result;
            return value1.CompareTo(other.value1);
        }

        public void Deserialize(BinaryReader reader)
        {
            value1 = reader.ReadUInt64();
            value2 = reader.ReadUInt64();
            value3 = reader.ReadUInt32();
        }

        /// <summary>
        /// Method Equals returns true if objects are equal, false otherwise
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            return Equals(obj as UInt160);
        }

        /// <summary>
        /// Method Equals returns true if objects are equal, false otherwise
        /// </summary>
        public bool Equals(UInt160 other)
        {
            if (other is null) return false;
            return value1 == other.value1
                && value2 == other.value2
                && value3 == other.value3;
        }

        public override int GetHashCode()
        {
            return (int)value1;
        }

        /// <summary>
        /// Method Parse receives a big-endian hex string and stores as a UInt160 little-endian 20-bytes array
        /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") should create UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
        /// </summary>
        public static UInt160 Parse(string value)
        {
            if (!TryParse(value, out var result)) throw new FormatException();
            return result;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
        }

        public override string ToString()
        {
            return string.Create(2 + (Length * 2), this, (buffer, that) =>
            {
                buffer[0] = '0';
                buffer[1] = 'x';
                value3.TryFormat(buffer[2..], out _, "x8");
                value2.TryFormat(buffer[10..], out _, "x16");
                value1.TryFormat(buffer[26..], out _, "x16");
            });
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
            if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                s = s.Substring(2);
            if (s.Length != Length * 2)
            {
                result = null;
                return false;
            }
            byte[] data = new byte[Length];
            for (int i = 0; i < Length; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[Length - i - 1]))
                {
                    result = null;
                    return false;
                }
            result = new UInt160(data);
            return true;
        }

        /// <summary>
        /// Returns true if left UInt160 is equals to right UInt160
        /// </summary>
        public static bool operator ==(UInt160 left, UInt160 right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true if left UIntBase is not equals to right UIntBase
        /// </summary>
        public static bool operator !=(UInt160 left, UInt160 right)
        {
            return !(left == right);
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
