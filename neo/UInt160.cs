using Neo.IO;
using System;
using System.Collections;
using System.Linq;
using System.IO;

namespace Neo
{
    public class UInt160 : IEquatable<UInt160>, IComparable<UInt160>, ISerializable
    {
        private static readonly int size = 20;

        public static readonly UInt160 Zero = new UInt160();

        private readonly byte[] buffer;

        public UInt160()
        {
            buffer = new byte[Size];
        }

        public UInt160(byte[] value) : this()
        {
            if (value.Length != Size)
                throw new ArgumentException();

            Array.Copy(value, buffer, buffer.Length);
        }

        public int Size => size;

        public bool Equals(UInt160 other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return buffer.SequenceEqual(other.buffer);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
            if (obj is UInt160 other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            return buffer.ToInt32(0);
        }

        public int CompareTo(UInt160 other)
        {
            return ((IStructuralComparable)buffer).CompareTo(other.buffer, StructuralComparisons.StructuralComparer);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(buffer);
        }

        public void Deserialize(BinaryReader reader)
        {
            reader.Read(buffer, 0, buffer.Length);
        }

        public byte[] ToArray()
        {
            return buffer.ToArray();
        }

        public override string ToString()
        {
            return "0x" + buffer.Reverse().ToHexString();
        }

        public static UInt160 Parse(string value)
        {
            return new UInt160(value.HexToBytes(size * 2).Reverse().ToArray());
        }

        public static bool TryParse(string s, out UInt160 result)
        {
            try
            {
                result = Parse(s);
                return true;
            }
            catch
            {
                result = Zero;
                return false;
            }
        }

        public static bool operator ==(UInt160 left, UInt160 right)
        {
            return ReferenceEquals(left, null) == false
                ? left.Equals(right)
                : ReferenceEquals(right, null);
        }

        public static bool operator !=(UInt160 left, UInt160 right)
        {
            return !(left == right);
        }

        public static bool operator >(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(UInt160 left, UInt160 right)
        {
            return left.CompareTo(right) <= 0;
        }
    }
}
