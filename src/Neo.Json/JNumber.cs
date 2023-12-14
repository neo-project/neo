// Copyright (C) 2015-2022 The Neo Project.
//
// The Neo.Json is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Globalization;
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
        /// Gets the value of the JSON token.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JNumber"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value of the JSON token.</param>
        public JNumber(double value = 0)
        {
            if (!double.IsFinite(value)) throw new FormatException();
            this.Value = value;
        }

        /// <summary>
        /// Converts the current JSON token to a boolean value.
        /// </summary>
        /// <returns><see langword="true"/> if value is not zero; otherwise, <see langword="false"/>.</returns>
        public override bool AsBoolean()
        {
            return Value != 0;
        }

        public override double AsNumber()
        {
            return Value;
        }

        public override string AsString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override double GetNumber() => Value;

        public override string ToString()
        {
            return AsString();
        }

        public override T AsEnum<T>(T defaultValue = default, bool ignoreCase = false)
        {
            Type enumType = typeof(T);
            object value;
            try
            {
                value = Convert.ChangeType(Value, enumType.GetEnumUnderlyingType());
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
            object result = Enum.ToObject(enumType, value);
            return Enum.IsDefined(enumType, result) ? (T)result : defaultValue;
        }

        public override T GetEnum<T>(bool ignoreCase = false)
        {
            Type enumType = typeof(T);
            object value;
            try
            {
                value = Convert.ChangeType(Value, enumType.GetEnumUnderlyingType());
            }
            catch (OverflowException)
            {
                throw new InvalidCastException();
            }
            object result = Enum.ToObject(enumType, value);
            if (!Enum.IsDefined(enumType, result))
                throw new InvalidCastException();
            return (T)result;
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(Value);
        }

        public override JNumber Clone()
        {
            return this;
        }

        public static implicit operator JNumber(double value)
        {
            return new JNumber(value);
        }

        public static implicit operator JNumber(long value)
        {
            return new JNumber(value);
        }

        public static bool operator ==(JNumber left, JNumber? right)
        {
            if (right is null) return false;
            return ReferenceEquals(left, right) || left.Value.Equals(right.Value);
        }

        public static bool operator !=(JNumber left, JNumber right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;

            var other = obj switch
            {
                JNumber jNumber => jNumber,
                uint u => new JNumber(u),
                int i => new JNumber(i),
                ulong ul => new JNumber(ul),
                long l => new JNumber(l),
                byte b => new JNumber(b),
                sbyte sb => new JNumber(sb),
                decimal d => new JNumber((double)d),
                float f => new JNumber(f),
                double d => new JNumber(d),
                _ => throw new ArgumentOutOfRangeException(nameof(obj), obj, null)
            };
            return other == this;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
