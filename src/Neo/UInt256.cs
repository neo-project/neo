// Copyright (C) 2015-2025 The Neo Project.
//
// UInt256.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo
{
    /// <summary>
    /// Represents a 256-bit unsigned integer.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public class UInt256 : IComparable<UInt256>, IEquatable<UInt256>, ISerializable, ISerializableSpan
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
        public UInt256() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt256"/> class.
        /// </summary>
        /// <param name="value">The value of the <see cref="UInt256"/>.</param>
        public UInt256(ReadOnlySpan<byte> value)
        {
            if (value.Length != Length)
                throw new FormatException($"Invalid length: {value.Length}");

            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref value1), Length);
            value.CopyTo(span);
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
        /// Gets a ReadOnlySpan that represents the current value in little-endian.
        /// </summary>
        /// <returns>A ReadOnlySpan that represents the current value in little-endian.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetSpan()
        {
            if (BitConverter.IsLittleEndian)
                return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref value1), Length);

            Span<byte> buffer = new byte[Length];
            Serialize(buffer);
            return buffer; // Keep the same output as Serialize when BigEndian
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

        public void Serialize(Span<byte> destination)
        {
            if (BitConverter.IsLittleEndian)
            {
                var buffer = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref value1), Length);
                buffer.CopyTo(destination);
            }
            else
            {
                const int IxValue2 = sizeof(ulong);
                const int IxValue3 = sizeof(ulong) * 2;
                const int IxValue4 = sizeof(ulong) * 3;

                Span<byte> buffer = stackalloc byte[Length];
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, value1);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[IxValue2..], value2);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[IxValue3..], value3);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[IxValue4..], value4);
                buffer.CopyTo(destination);
            }
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
        public static bool TryParse(string s, [NotNullWhen(true)] out UInt256 result)
        {
            result = null;
            var data = s.AsSpan(); // AsSpan is null safe
            if (data.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                data = data[2..];

            if (data.Length != Length * 2) return false;

            try
            {
                result = new UInt256(data.HexToBytesReversed());
                return true;
            }
            catch
            {
                return false;
            }
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
