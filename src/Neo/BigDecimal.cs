// Copyright (C) 2015-2025 The Neo Project.
//
// BigDecimal.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System;
using System.Numerics;

namespace Neo
{
    /// <summary>
    /// Represents a fixed-point number of arbitrary precision.
    /// </summary>
    public readonly struct BigDecimal : IComparable<BigDecimal>, IEquatable<BigDecimal>
    {
        private readonly BigInteger _value;
        private readonly byte _decimals;

        /// <summary>
        /// The <see cref="BigInteger"/> value of the number.
        /// </summary>
        public readonly BigInteger Value => _value;

        /// <summary>
        /// The number of decimal places for this number.
        /// </summary>
        public readonly byte Decimals => _decimals;

        /// <summary>
        /// The sign of the number.
        /// </summary>
        public readonly int Sign => _value.Sign;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDecimal"/> struct.
        /// </summary>
        /// <param name="value">The <see cref="BigInteger"/> value of the number.</param>
        /// <param name="decimals">The number of decimal places for this number.</param>
        public BigDecimal(BigInteger value, byte decimals)
        {
            _value = value;
            _decimals = decimals;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDecimal"/> struct with the value of <see cref="decimal"/>.
        /// </summary>
        /// <param name="value">The value of the number.</param>
        public unsafe BigDecimal(decimal value)
        {
            Span<int> span = stackalloc int[4];
            span = decimal.GetBits(value);
            fixed (int* p = span)
            {
                ReadOnlySpan<byte> buffer = new(p, 16);
                _value = new BigInteger(buffer[..12], isUnsigned: true);
                if (buffer[15] != 0) _value = -_value;
                _decimals = buffer[14];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDecimal"/> struct with the value of <see cref="decimal"/>.
        /// </summary>
        /// <param name="value">The value of the number.</param>
        /// <param name="decimals">The number of decimal places for this number.</param>
        public unsafe BigDecimal(decimal value, byte decimals)
        {
            Span<int> span = stackalloc int[4];
            span = decimal.GetBits(value);
            fixed (int* p = span)
            {
                ReadOnlySpan<byte> buffer = new(p, 16);
                _value = new BigInteger(buffer[..12], isUnsigned: true);
                if (buffer[14] > decimals)
                    throw new ArgumentException(null, nameof(value));
                else if (buffer[14] < decimals)
                    _value *= BigInteger.Pow(10, decimals - buffer[14]);
                if (buffer[15] != 0)
                    _value = -_value;
            }
            _decimals = decimals;
        }

        /// <summary>
        /// Changes the decimals of the <see cref="BigDecimal"/>.
        /// </summary>
        /// <param name="decimals">The new decimals field.</param>
        /// <returns>The <see cref="BigDecimal"/> that has the new number of decimal places.</returns>
        public readonly BigDecimal ChangeDecimals(byte decimals)
        {
            if (_decimals == decimals) return this;
            BigInteger value;
            if (_decimals < decimals)
            {
                value = _value * BigInteger.Pow(10, decimals - _decimals);
            }
            else
            {
                var divisor = BigInteger.Pow(10, _decimals - decimals);
                value = BigInteger.DivRem(_value, divisor, out var remainder);
                if (remainder > BigInteger.Zero)
                    throw new ArgumentOutOfRangeException(nameof(decimals));
            }
            return new BigDecimal(value, decimals);
        }

        /// <summary>
        /// Parses a <see cref="BigDecimal"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="s">A number represented by a <see cref="string"/>.</param>
        /// <param name="decimals">The number of decimal places for this number.</param>
        /// <returns>The parsed <see cref="BigDecimal"/>.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
        public static BigDecimal Parse(string s, byte decimals)
        {
            if (!TryParse(s, decimals, out var result))
                throw new FormatException();
            return result;
        }

        /// <summary>
        /// Gets a <see cref="string"/> representing the number.
        /// </summary>
        /// <returns>The <see cref="string"/> representing the number.</returns>
        public override readonly string ToString()
        {
            var divisor = BigInteger.Pow(10, _decimals);
            var result = BigInteger.DivRem(_value, divisor, out var remainder);
            if (remainder == 0) return result.ToString();
            return $"{result}.{remainder.ToString("d" + _decimals)}".TrimEnd('0');
        }

        /// <summary>
        /// Parses a <see cref="BigDecimal"/> from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="s">A number represented by a <see cref="string"/>.</param>
        /// <param name="decimals">The number of decimal places for this number.</param>
        /// <param name="result">The parsed <see cref="BigDecimal"/>.</param>
        /// <returns><see langword="true"/> if a number is successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string s, byte decimals, out BigDecimal result)
        {
            var e = 0;
            var index = s.IndexOfAny(['e', 'E']);
            if (index >= 0)
            {
                if (!sbyte.TryParse(s[(index + 1)..], out var e_temp))
                {
                    result = default;
                    return false;
                }
                e = e_temp;
                s = s[..index];
            }
            index = s.IndexOf('.');
            if (index >= 0)
            {
                s = s.TrimEnd('0');
                e -= s.Length - index - 1;
                s = s.Remove(index, 1);
            }
            var ds = e + decimals;
            if (ds < 0)
            {
                result = default;
                return false;
            }
            if (ds > 0)
                s += new string('0', ds);
            if (!BigInteger.TryParse(s, out var value))
            {
                result = default;
                return false;
            }
            result = new BigDecimal(value, decimals);
            return true;
        }

        public readonly int CompareTo(BigDecimal other)
        {
            BigInteger left = _value, right = other._value;
            if (_decimals < other._decimals)
                left *= BigInteger.Pow(10, other._decimals - _decimals);
            else if (_decimals > other._decimals)
                right *= BigInteger.Pow(10, _decimals - other._decimals);
            return left.CompareTo(right);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is not BigDecimal @decimal) return false;
            return Equals(@decimal);
        }

        public readonly bool Equals(BigDecimal other)
        {
            return CompareTo(other) == 0;
        }

        public override readonly int GetHashCode()
        {
            var divisor = BigInteger.Pow(10, _decimals);
            var result = BigInteger.DivRem(_value, divisor, out var remainder);
            return HashCode.Combine(result, remainder);
        }

        public static bool operator ==(BigDecimal left, BigDecimal right)
        {
            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(BigDecimal left, BigDecimal right)
        {
            return left.CompareTo(right) != 0;
        }

        public static bool operator <(BigDecimal left, BigDecimal right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(BigDecimal left, BigDecimal right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(BigDecimal left, BigDecimal right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(BigDecimal left, BigDecimal right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
