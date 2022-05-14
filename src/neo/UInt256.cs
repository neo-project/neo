// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Neo
{
    /// <summary>
    /// Represents a 256-bit unsigned integer.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public class UInt256 : IComparable<UInt256>, IEquatable<UInt256>, ISerializable
    {
        /// <summary>
        /// The length of <see cref="UInt256"/> values.
        /// </summary>
        public const int Length = 32;

        /// <summary>
        /// Represents 0.
        /// </summary>
        public static readonly UInt256 Zero = new();

        [FieldOffset(0)] private ulong value1;
        [FieldOffset(8)] private ulong value2;
        [FieldOffset(16)] private ulong value3;
        [FieldOffset(24)] private ulong value4;

        public int Size => Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt256"/> class.
        /// </summary>
        public UInt256()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt256"/> class.
        /// </summary>
        /// <param name="value">The value of the <see cref="UInt256"/>.</param>
        public unsafe UInt256(ReadOnlySpan<byte> value)
        {
            if (value.Length != Length) throw new FormatException();
            fixed (ulong* p = &value1)
            {
                Span<byte> dst = new(p, Length);
                value[..Length].CopyTo(dst);
            }
        }

        public int CompareTo(UInt256 other)
        {
            int result = value4.CompareTo(other.value4);
            if (result != 0) return result;
            result = value3.CompareTo(other.value3);
            if (result != 0) return result;
            result = value2.CompareTo(other.value2);
            if (result != 0) return result;
            return value1.CompareTo(other.value1);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            value1 = reader.ReadUInt64();
            value2 = reader.ReadUInt64();
            value3 = reader.ReadUInt64();
            value4 = reader.ReadUInt64();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            return Equals(obj as UInt256);
        }

        public bool Equals(UInt256 other)
        {
            if (other is null) return false;
            return value1 == other.value1
                && value2 == other.value2
                && value3 == other.value3
                && value4 == other.value4;
        }

        public override int GetHashCode()
        {
            return (int)value1;
        }

        /// <summary>
        /// Parses an <see cref="UInt256"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">An <see cref="UInt256"/> represented by a <see cref="string"/>.</param>
        /// <returns>The parsed <see cref="UInt256"/>.</returns>
        /// <exception cref="FormatException"><paramref name="value"/> is not in the correct format.</exception>
        public static UInt256 Parse(string value)
        {
            if (!TryParse(value, out var result)) throw new FormatException();
            return result;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
        }

        public override string ToString()
        {
            return "0x" + this.ToArray().ToHexString(reverse: true);
        }

        /// <summary>
        /// Parses an <see cref="UInt256"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="s">An <see cref="UInt256"/> represented by a <see cref="string"/>.</param>
        /// <param name="result">The parsed <see cref="UInt256"/>.</param>
        /// <returns><see langword="true"/> if an <see cref="UInt256"/> is successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string s, out UInt256 result)
        {
            if (s == null)
            {
                result = null;
                return false;
            }
            if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                s = s[2..];
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
            result = new UInt256(data);
            return true;
        }

        public static bool operator ==(UInt256 left, UInt256 right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(UInt256 left, UInt256 right)
        {
            return !(left == right);
        }

        public static bool operator >(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) <= 0;
        }
    }
}
