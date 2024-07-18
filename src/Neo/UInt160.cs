// Copyright (C) 2015-2024 The Neo Project.
//
// UInt160.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo
{
    /// <summary>
    /// Represents a 160-bit unsigned integer.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public class UInt160 : IComparable<UInt160>, IEquatable<UInt160>, ISerializable
    {
        /// <summary>
        /// The length of <see cref="UInt160"/> values.
        /// </summary>
        public const int Length = 20;

        /// <summary>
        /// Represents 0.
        /// </summary>
        public static UInt160 Zero => new();

        [FieldOffset(0)] private ulong _value1;
        [FieldOffset(8)] private ulong _value2;
        [FieldOffset(16)] private uint _value3;

        public int Size => Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt160"/> class.
        /// </summary>
        public UInt160() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt160"/> class.
        /// </summary>
        /// <param name="value">The value of the <see cref="UInt160"/>.</param>
        public UInt160(ReadOnlySpan<byte> value)
        {
            if (value.Length != Length)
                throw new FormatException();

            var bytes = value.ToArray();
            _value1 = Unsafe.As<byte, ulong>(ref bytes[0]);
            _value2 = Unsafe.As<byte, ulong>(ref bytes[8]);
            _value3 = Unsafe.As<byte, uint>(ref bytes[16]);
        }

        public int CompareTo(UInt160 other)
        {
            var result = _value3.CompareTo(other._value3);
            if (result != 0) return result;
            result = _value2.CompareTo(other._value2);
            if (result != 0) return result;
            return _value1.CompareTo(other._value1);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            _value1 = reader.ReadUInt64();
            _value2 = reader.ReadUInt64();
            _value3 = reader.ReadUInt32();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as UInt160;
            if (other == null)
                return false;
            return Equals(other);
        }

        public bool Equals(UInt160 other)
        {
            if (other == null) return false;
            return _value1 == other._value1 &&
                _value2 == other._value2 &&
                _value3 == other._value3;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value1, _value2, _value3);
        }

        /// <summary>
        /// Parses an <see cref="UInt160"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">An <see cref="UInt160"/> represented by a <see cref="string"/>.</param>
        /// <returns>The parsed <see cref="UInt160"/>.</returns>
        /// <exception cref="FormatException"><paramref name="value"/> is not in the correct format.</exception>
        public static UInt160 Parse(string value)
        {
            if (!TryParse(value, out var result)) throw new FormatException();
            return result;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_value1);
            writer.Write(_value2);
            writer.Write(_value3);
        }

        public override string ToString()
        {
            return "0x" + this.ToArray().ToHexString();
        }

        /// <summary>
        /// Parses an <see cref="UInt160"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">An <see cref="UInt160"/> represented by a <see cref="string"/>.</param>
        /// <param name="result">The parsed <see cref="UInt160"/>.</param>
        /// <returns><see langword="true"/> if an <see cref="UInt160"/> is successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string str, out UInt160 result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(str)) return false;

            if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                str = str[2..];

                if (str.Length != Length * 2) return false;

                var data = Enumerable.Range(0, Length)
                    .Select(s => Convert.ToByte(str.Substring(s * 2, 2), 16))
                    .ToArray();

                result = new(data);
                return true;
            }
            return false;
        }

        public static implicit operator UInt160(string s)
        {
            return Parse(s);
        }

        public static implicit operator UInt160(byte[] b)
        {
            return new UInt160(b);
        }

        public static bool operator ==(UInt160 left, UInt160 right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null)
                return Equals(left, right);
            return left.Equals(right);
        }

        public static bool operator !=(UInt160 left, UInt160 right)
        {
            if (ReferenceEquals(left, right)) return false;
            if (left is null || right is null)
                return !Equals(left, right);
            return !left.Equals(right);
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
