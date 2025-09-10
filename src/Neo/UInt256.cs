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
    public class UInt256 : IComparable, IComparable<UInt256>, IEquatable<UInt256>, ISerializable, ISerializableSpan
    {
        /// <summary>
        /// The length of <see cref="UInt256"/> values.
        /// </summary>
        public const int Length = 32;

        /// <summary>
        /// Represents 0.
        /// </summary>
        public static readonly UInt256 Zero = new();

        [FieldOffset(0)] private ulong _value1;
        [FieldOffset(8)] private ulong _value2;
        [FieldOffset(16)] private ulong _value3;
        [FieldOffset(24)] private ulong _value4;

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
                throw new FormatException($"Invalid UInt256 length: expected {Length} bytes, but got {value.Length} bytes. UInt256 values must be exactly 32 bytes long.");

            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref _value1), Length);
            value.CopyTo(span);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(obj, this)) return 0;
            return CompareTo(obj as UInt256);
        }

        public int CompareTo(UInt256 other)
        {
            var result = _value4.CompareTo(other._value4);
            if (result != 0) return result;
            result = _value3.CompareTo(other._value3);
            if (result != 0) return result;
            result = _value2.CompareTo(other._value2);
            if (result != 0) return result;
            return _value1.CompareTo(other._value1);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            _value1 = reader.ReadUInt64();
            _value2 = reader.ReadUInt64();
            _value3 = reader.ReadUInt64();
            _value4 = reader.ReadUInt64();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            return Equals(obj as UInt256);
        }

        public bool Equals(UInt256 other)
        {
            if (other is null) return false;
            return _value1 == other._value1
                && _value2 == other._value2
                && _value3 == other._value3
                && _value4 == other._value4;
        }

        public override int GetHashCode()
        {
            return (int)_value1;
        }

        /// <summary>
        /// Gets a ReadOnlySpan that represents the current value in little-endian.
        /// </summary>
        /// <returns>A ReadOnlySpan that represents the current value in little-endian.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetSpan()
        {
            if (BitConverter.IsLittleEndian)
                return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref _value1), Length);

            return GetSpanLittleEndian();
        }

        /// <summary>
        /// Get the output as Serialize when BigEndian
        /// </summary>
        /// <returns>A Span that represents the ourput as Serialize when BigEndian.</returns>
        internal Span<byte> GetSpanLittleEndian()
        {
            Span<byte> buffer = new byte[Length];
            SafeSerialize(buffer);
            return buffer; // Keep the same output as Serialize when BigEndian
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_value1);
            writer.Write(_value2);
            writer.Write(_value3);
            writer.Write(_value4);
        }

        /// <inheritdoc/>
        public void Serialize(Span<byte> destination)
        {
            if (BitConverter.IsLittleEndian)
            {
                var buffer = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref _value1), Length);
                buffer.CopyTo(destination);
            }
            else
            {
                SafeSerialize(destination);
            }
        }

        // internal for testing, don't use it directly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SafeSerialize(Span<byte> destination)
        {
            // Avoid partial write and keep the same Exception as before if the buffer is too small
            if (destination.Length < Length)
                throw new ArgumentException($"Destination buffer size ({destination.Length} bytes) is too small to serialize UInt256. Required size is {Length} bytes.", nameof(destination));

            const int IxValue2 = sizeof(ulong);
            const int IxValue3 = sizeof(ulong) * 2;
            const int IxValue4 = sizeof(ulong) * 3;
            BinaryPrimitives.WriteUInt64LittleEndian(destination, _value1);
            BinaryPrimitives.WriteUInt64LittleEndian(destination[IxValue2..], _value2);
            BinaryPrimitives.WriteUInt64LittleEndian(destination[IxValue3..], _value3);
            BinaryPrimitives.WriteUInt64LittleEndian(destination[IxValue4..], _value4);
        }

        public override string ToString()
        {
            return "0x" + GetSpan().ToHexString(reverse: true);
        }

        /// <summary>
        /// Parses an <see cref="UInt256"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">An <see cref="UInt256"/> represented by a <see cref="string"/>.</param>
        /// <param name="result">The parsed <see cref="UInt256"/>.</param>
        /// <returns>
        /// <see langword="true"/> if an <see cref="UInt256"/> is successfully parsed; otherwise, <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse(string value, [NotNullWhen(true)] out UInt256 result)
        {
            result = null;
            var data = value.AsSpan().TrimStartIgnoreCase("0x");
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

        /// <summary>
        /// Parses an <see cref="UInt256"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">An <see cref="UInt256"/> represented by a <see cref="string"/>.</param>
        /// <returns>The parsed <see cref="UInt256"/>.</returns>
        /// <exception cref="FormatException"><paramref name="value"/> is not in the correct format.</exception>
        public static UInt256 Parse(string value)
        {
            var data = value.AsSpan().TrimStartIgnoreCase("0x");
            if (data.Length != Length * 2)
                throw new FormatException($"Invalid UInt256 string format: expected {Length * 2} hexadecimal characters, but got {data.Length}. UInt256 values must be represented as 64 hexadecimal characters (with or without '0x' prefix).");
            return new UInt256(data.HexToBytesReversed());
        }

        public static implicit operator UInt256(string s)
        {
            return Parse(s);
        }

        public static implicit operator UInt256(byte[] b)
        {
            return new UInt256(b);
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
