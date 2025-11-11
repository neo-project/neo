// Copyright (C) 2015-2025 The Neo Project.
//
// UInt160.cs file belongs to the neo project and is free
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
    /// Represents a 160-bit unsigned integer.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public class UInt160 : IComparable, IComparable<UInt160>, IEquatable<UInt160>, ISerializable, ISerializableSpan
    {
        /// <summary>
        /// The length of <see cref="UInt160"/> values.
        /// </summary>
        public const int Length = 20;

        /// <summary>
        /// Represents 0.
        /// </summary>
        public readonly static UInt160 Zero = new();

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
                throw new FormatException($"Invalid UInt160 length: expected {Length} bytes, but got {value.Length} bytes. UInt160 values must be exactly 20 bytes long.");

            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref _value1), Length);
            value.CopyTo(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(obj, this)) return 0;
            return CompareTo(obj as UInt160);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(UInt160? other)
        {
            if (other is null) return 1;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            return Equals(obj as UInt160);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UInt160? other)
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

        internal Span<byte> GetSpanLittleEndian()
        {
            Span<byte> buffer = new byte[Length];
            SafeSerialize(buffer);
            return buffer; // Keep the same output as Serialize when BigEndian
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
                throw new ArgumentException($"Destination buffer size ({destination.Length} bytes) is too small to serialize UInt160. Required size is {Length} bytes.", nameof(destination));

            const int IxValue2 = sizeof(ulong);
            const int IxValue3 = sizeof(ulong) * 2;
            BinaryPrimitives.WriteUInt64LittleEndian(destination, _value1);
            BinaryPrimitives.WriteUInt64LittleEndian(destination[IxValue2..], _value2);
            BinaryPrimitives.WriteUInt32LittleEndian(destination[IxValue3..], _value3);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_value1);
            writer.Write(_value2);
            writer.Write(_value3);
        }

        public override string ToString()
        {
            return "0x" + GetSpan().ToHexString(reverse: true);
        }

        /// <summary>
        /// Parses an <see cref="UInt160"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">An <see cref="UInt160"/> represented by a <see cref="string"/>.</param>
        /// <param name="result">The parsed <see cref="UInt160"/>.</param>
        /// <returns>
        /// <see langword="true"/> if an <see cref="UInt160"/> is successfully parsed; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryParse(string value, [NotNullWhen(true)] out UInt160? result)
        {
            result = null;
            var data = value.AsSpan().TrimStartIgnoreCase("0x");
            if (data.Length != Length * 2) return false;
            try
            {
                result = new UInt160(data.HexToBytesReversed());
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Parses an <see cref="UInt160"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">An <see cref="UInt160"/> represented by a <see cref="string"/>.</param>
        /// <returns>The parsed <see cref="UInt160"/>.</returns>
        /// <exception cref="FormatException"><paramref name="value"/> is not in the correct format.</exception>
        public static UInt160 Parse(string value)
        {
            var data = value.AsSpan().TrimStartIgnoreCase("0x");
            if (data.Length != Length * 2)
                throw new FormatException($"Invalid UInt160 string format: expected {Length * 2} hexadecimal characters, but got {data.Length}. UInt160 values must be represented as 40 hexadecimal characters (with or without '0x' prefix).");
            return new UInt160(data.HexToBytesReversed());
        }

        public static implicit operator UInt160(string s)
        {
            return Parse(s);
        }

        public static implicit operator UInt160(byte[] b)
        {
            return new UInt160(b);
        }

        public static bool operator ==(UInt160? left, UInt160? right)
        {
            if (left is null || right is null)
                return Equals(left, right);
            return left.Equals(right);
        }

        public static bool operator !=(UInt160? left, UInt160? right)
        {
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
