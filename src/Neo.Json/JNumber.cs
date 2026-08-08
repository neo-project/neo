// Copyright (C) 2015-2026 The Neo Project.
//
// JNumber.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace Neo.Json
{
    /// <summary>
    /// Represents a JSON number.
    /// </summary>
    public class JNumber : JToken
    {
        /// <summary>
        /// Represents the largest safe integer in JSON.
        /// </summary>
        public static readonly long MAX_SAFE_INTEGER = (long)Math.Pow(2, 53) - 1;

        /// <summary>
        /// Represents the smallest safe integer in JSON.
        /// </summary>
        public static readonly long MIN_SAFE_INTEGER = -MAX_SAFE_INTEGER;

        /// <summary>
        /// When non-null, the exact integer value. Used for integers outside the IEEE-754
        /// safe integer range (or constructed from <see cref="BigInteger"/> outside that range)
        /// so they round-trip without precision loss.
        /// </summary>
        private readonly BigInteger? _integer;

        private readonly double _double;

        /// <summary>
        /// Gets the value of the JSON token as a floating-point number.
        /// For large integers this may lose precision; use <see cref="TryGetBigInteger"/> or
        /// <see cref="GetBigInteger"/> when an exact integer is required.
        /// </summary>
        public double Value => _integer is BigInteger bi ? (double)bi : _double;

        /// <summary>
        /// Gets whether this number uses the exact <see cref="BigInteger"/> storage path
        /// (values outside the IEEE-754 safe integer range). Safe-range integers remain double-backed.
        /// </summary>
        public bool HasExactBigInteger => _integer.HasValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="JNumber"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value of the JSON token.</param>
        public JNumber(double value = 0)
        {
            if (!double.IsFinite(value)) throw new FormatException($"value is not finite: {value}");
            _double = value;
            _integer = null;
        }

        /// <summary>
        /// Creates a <see cref="JNumber"/> from an exact integer.
        /// Values inside the safe integer range are stored as <see cref="double"/> for compatibility;
        /// larger magnitudes keep a <see cref="BigInteger"/> so JSON write does not lose precision.
        /// Prefer implicit conversion from <see cref="BigInteger"/> in assignment contexts.
        /// </summary>
        /// <param name="value">The integer value of the JSON token.</param>
        public static JNumber FromBigInteger(BigInteger value)
        {
            if (value >= MIN_SAFE_INTEGER && value <= MAX_SAFE_INTEGER)
                return new JNumber((double)value);
            return new JNumber(value, exact: true);
        }

        /// <summary>
        /// Private constructor for exact integers outside the double-only path.
        /// Not public: a public <see cref="BigInteger"/> constructor would make
        /// <c>new JNumber(1)</c> ambiguous with <see cref="JNumber(double)"/>.
        /// </summary>
        private JNumber(BigInteger value, bool exact)
        {
            _ = exact;
            _double = 0; // unused when _integer is set
            _integer = value;
        }

        /// <summary>
        /// Converts the current JSON token to a boolean value.
        /// </summary>
        /// <returns><see langword="true"/> if value is not zero; otherwise, <see langword="false"/>.</returns>
        public override bool AsBoolean()
        {
            if (_integer is BigInteger bi) return !bi.IsZero;
            return _double != 0;
        }

        public override double AsNumber()
        {
            return Value;
        }

        public override string AsString()
        {
            if (_integer is BigInteger bi)
                return bi.ToString(CultureInfo.InvariantCulture);
            return _double.ToString(CultureInfo.InvariantCulture);
        }

        public override double GetNumber() => Value;

        /// <summary>
        /// Tries to get the value when this token uses exact <see cref="BigInteger"/> storage
        /// (the new path for integers outside the safe range). Does not convert double-backed numbers.
        /// </summary>
        /// <param name="value">When successful, the exact integer.</param>
        /// <returns><see langword="true"/> if stored as exact <see cref="BigInteger"/>; otherwise <see langword="false"/>.</returns>
        public bool TryGetExactBigInteger(out BigInteger value)
        {
            if (_integer is BigInteger bi)
            {
                value = bi;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get an integer value from this token (exact storage or integral double).
        /// </summary>
        /// <param name="value">When successful, the integer value.</param>
        /// <returns><see langword="true"/> if the number is an integer; otherwise <see langword="false"/>.</returns>
        public bool TryGetBigInteger(out BigInteger value)
        {
            if (TryGetExactBigInteger(out value))
                return true;

            if (_double % 1 != 0)
            {
                value = default;
                return false;
            }

            try
            {
                value = (BigInteger)_double;
                return true;
            }
            catch (OverflowException)
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Gets the integer value of this token.
        /// </summary>
        /// <returns>The integer value.</returns>
        /// <exception cref="InvalidCastException">The number is not an integer.</exception>
        public BigInteger GetBigInteger()
        {
            if (TryGetBigInteger(out var value)) return value;
            throw new InvalidCastException("The JSON number is not an integer.");
        }

        public override string ToString()
        {
            return AsString();
        }

        public override T AsEnum<T>(T defaultValue = default, bool ignoreCase = false)
        {
            var enumType = typeof(T);
            object value;
            try
            {
                value = Convert.ChangeType(Value, enumType.GetEnumUnderlyingType());
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
            var result = Enum.ToObject(enumType, value);
            return Enum.IsDefined(enumType, result) ? (T)result : defaultValue;
        }

        public override T GetEnum<T>(bool ignoreCase = false)
        {
            var enumType = typeof(T);
            object value;
            try
            {
                value = Convert.ChangeType(Value, enumType.GetEnumUnderlyingType());
            }
            catch (OverflowException)
            {
                throw new InvalidCastException($"The value is out of range for the enum {enumType.FullName}");
            }

            var result = Enum.ToObject(enumType, value);
            if (!Enum.IsDefined(enumType, result))
                throw new InvalidCastException($"The value is not defined in the enum {enumType.FullName}");
            return (T)result;
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            if (_integer is BigInteger bi)
            {
                // Write exact integer digits (valid JSON number token) without double conversion.
                writer.WriteRawValue(bi.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
            }
            else
            {
                writer.WriteNumberValue(_double);
            }
        }

        public override JToken Clone()
        {
            return this;
        }

        public static implicit operator JNumber(double value)
        {
            return new JNumber(value);
        }

        public static implicit operator JNumber(long value)
        {
            // Unchanged historical path: long → double (safe for consensus / existing callers).
            // Use <see cref="FromBigInteger"/> or BigInteger implicit for exact large integers.
            return new JNumber(value);
        }

        public static implicit operator JNumber(BigInteger value)
        {
            return FromBigInteger(value);
        }

        public static bool operator ==(JNumber left, JNumber? right)
        {
            if (right is null) return false;
            return ReferenceEquals(left, right) || left.Equals(right);
        }

        public static bool operator !=(JNumber left, JNumber right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj switch
            {
                JNumber jNumber => Equals(jNumber),
                BigInteger bi => EqualsBigInteger(bi),
                uint u => EqualsNumber(u),
                int i => EqualsNumber(i),
                ulong ul => EqualsBigInteger(ul),
                long l => EqualsBigInteger(l),
                byte b => EqualsNumber(b),
                sbyte sb => EqualsNumber(sb),
                short s => EqualsNumber(s),
                ushort us => EqualsNumber(us),
                decimal d => EqualsNumber((double)d),
                float f => EqualsNumber(f),
                double d => EqualsNumber(d),
                _ => throw new ArgumentOutOfRangeException(nameof(obj), obj, null)
            };
        }

        private bool Equals(JNumber other)
        {
            if (_integer is BigInteger leftBi)
            {
                if (other._integer is BigInteger rightBi)
                    return leftBi == rightBi;
                return other.TryGetBigInteger(out var otherBi) && leftBi == otherBi;
            }

            if (other._integer is BigInteger otherOnly)
            {
                return TryGetBigInteger(out var thisBi) && thisBi == otherOnly;
            }

            return _double.Equals(other._double);
        }

        private bool EqualsBigInteger(BigInteger value)
        {
            if (_integer is BigInteger bi) return bi == value;
            if (_double % 1 != 0) return false;
            if (value >= MIN_SAFE_INTEGER && value <= MAX_SAFE_INTEGER)
                return _double.Equals((double)value);
            // double cannot represent all integers outside the safe range.
            return TryGetBigInteger(out var thisBi) && thisBi == value;
        }

        private bool EqualsNumber(double value)
        {
            if (_integer is BigInteger bi)
            {
                if (value % 1 != 0) return false;
                if (value > MAX_SAFE_INTEGER || value < MIN_SAFE_INTEGER)
                    return false; // cannot trust double equality outside safe range
                return bi == (long)value;
            }
            return _double.Equals(value);
        }

        public override int GetHashCode()
        {
            if (_integer is BigInteger bi)
                return bi.GetHashCode();
            return _double.GetHashCode();
        }
    }
}
