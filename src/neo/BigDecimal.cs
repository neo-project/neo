// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;

namespace Neo
{
    /// <summary>
    /// Represents a fixed-point number of arbitrary precision.
    /// </summary>
    public struct BigDecimal : IComparable<BigDecimal>, IEquatable<BigDecimal>
    {
        private readonly BigInteger value;
        private readonly byte decimals;

        /// <summary>
        /// The <see cref="BigInteger"/> value of the number.
        /// </summary>
        public BigInteger Value => value;

        /// <summary>
        /// The number of decimal places for this number.
        /// </summary>
        public byte Decimals => decimals;

        /// <summary>
        /// The sign of the number.
        /// </summary>
        public int Sign => value.Sign;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDecimal"/> struct.
        /// </summary>
        /// <param name="value">The <see cref="BigInteger"/> value of the number.</param>
        /// <param name="decimals">The number of decimal places for this number.</param>
        public BigDecimal(BigInteger value, byte decimals)
        {
            this.value = value;
            this.decimals = decimals;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDecimal"/> struct with the value of <see cref="decimal"/>.
        /// </summary>
        /// <param name="value">The value of the number.</param>
        public unsafe BigDecimal(decimal value)
        {
            ReadOnlySpan<byte> buffer = new(&value, sizeof(decimal));
            this.decimals = buffer[14];
            this.value = new BigInteger(decimal.Multiply((decimal)Math.Pow(10, decimals), value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDecimal"/> struct with the value of <see cref="decimal"/>.
        /// </summary>
        /// <param name="value">The value of the number.</param>
        /// <param name="decimals">The number of decimal places for this number.</param>
        public unsafe BigDecimal(decimal value, byte decimals)
        {
            ReadOnlySpan<byte> buffer = new(&value, sizeof(decimal));
            if (buffer[14] > decimals) throw new ArgumentException(null, nameof(value));
            this.value = new BigInteger(decimal.Multiply((decimal)Math.Pow(10, decimals), value));
            this.decimals = decimals;
        }

        /// <summary>
        /// Changes the decimals of the <see cref="BigDecimal"/>.
        /// </summary>
        /// <param name="decimals">The new decimals field.</param>
        /// <returns>The <see cref="BigDecimal"/> that has the new number of decimal places.</returns>
        public BigDecimal ChangeDecimals(byte decimals)
        {
            if (this.decimals == decimals) return this;
            BigInteger value;
            if (this.decimals < decimals)
            {
                value = this.value * BigInteger.Pow(10, decimals - this.decimals);
            }
            else
            {
                BigInteger divisor = BigInteger.Pow(10, this.decimals - decimals);
                value = BigInteger.DivRem(this.value, divisor, out BigInteger remainder);
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
            if (!TryParse(s, decimals, out BigDecimal result))
                throw new FormatException();
            return result;
        }

        /// <summary>
        /// Gets a <see cref="string"/> representing the number.
        /// </summary>
        /// <returns>The <see cref="string"/> representing the number.</returns>
        public override string ToString()
        {
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger result = BigInteger.DivRem(value, divisor, out BigInteger remainder);
            if (remainder == 0) return result.ToString();
            return $"{result}.{remainder.ToString("d" + decimals)}".TrimEnd('0');
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
            int e = 0;
            int index = s.IndexOfAny(new[] { 'e', 'E' });
            if (index >= 0)
            {
                if (!sbyte.TryParse(s[(index + 1)..], out sbyte e_temp))
                {
                    result = default;
                    return false;
                }
                e = e_temp;
                s = s.Substring(0, index);
            }
            index = s.IndexOf('.');
            if (index >= 0)
            {
                s = s.TrimEnd('0');
                e -= s.Length - index - 1;
                s = s.Remove(index, 1);
            }
            int ds = e + decimals;
            if (ds < 0)
            {
                result = default;
                return false;
            }
            if (ds > 0)
                s += new string('0', ds);
            if (!BigInteger.TryParse(s, out BigInteger value))
            {
                result = default;
                return false;
            }
            result = new BigDecimal(value, decimals);
            return true;
        }

        public int CompareTo(BigDecimal other)
        {
            BigInteger left = value, right = other.value;
            if (decimals < other.decimals)
                left *= BigInteger.Pow(10, other.decimals - decimals);
            else if (decimals > other.decimals)
                right *= BigInteger.Pow(10, decimals - other.decimals);
            return left.CompareTo(right);
        }

        public override bool Equals(object obj)
        {
            if (obj is not BigDecimal @decimal) return false;
            return Equals(@decimal);
        }

        public bool Equals(BigDecimal other)
        {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value, decimals);
        }
    }
}
