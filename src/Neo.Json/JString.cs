// Copyright (C) 2015-2024 The Neo Project.
//
// JString.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Globalization;
using System.Text.Json;

namespace Neo.Json
{
    /// <summary>
    /// Represents a JSON string.
    /// </summary>
    public class JString : JToken
    {
        /// <summary>
        /// Gets the value of the JSON token.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JString"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value of the JSON token.</param>
        public JString(string value)
        {
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Converts the current JSON token to a boolean value.
        /// </summary>
        /// <returns><see langword="true"/> if value is not empty; otherwise, <see langword="false"/>.</returns>
        public override bool AsBoolean()
        {
            return !string.IsNullOrEmpty(Value);
        }

        public override double AsNumber()
        {
            if (string.IsNullOrEmpty(Value)) return 0;
            return double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : double.NaN;
        }

        public override string AsString()
        {
            return Value;
        }

        public override string GetString() => Value;

        public override T AsEnum<T>(T defaultValue = default, bool ignoreCase = false)
        {
            try
            {
                return Enum.Parse<T>(Value, ignoreCase);
            }
            catch
            {
                return defaultValue;
            }
        }

        public override T GetEnum<T>(bool ignoreCase = false)
        {
            T result = Enum.Parse<T>(Value, ignoreCase);
            if (!Enum.IsDefined(result)) throw new InvalidCastException();
            return result;
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteStringValue(Value);
        }

        public override JString Clone()
        {
            return this;
        }

        public static implicit operator JString(Enum value)
        {
            return new JString(value.ToString());
        }

        public static implicit operator JString?(string? value)
        {
            return value == null ? null : new JString(value);
        }

        public static bool operator ==(JString left, JString? right)
        {
            if (right is null) return false;
            return ReferenceEquals(left, right) || left.Value.Equals(right.Value);
        }

        public static bool operator !=(JString left, JString right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is JString other)
            {
                return this == other;
            }
            if (obj is string str)
            {
                return this.Value == str;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
