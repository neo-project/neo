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
using System.Globalization;
using System.Text.Json;

namespace Neo.IO.Json
{
    /// <summary>
    /// Represents a JSON number.
    /// </summary>
    public class JNumber : JObject
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
        /// Gets the value of the JSON object.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JNumber"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value of the JSON object.</param>
        public JNumber(double value = 0)
        {
            if (!double.IsFinite(value)) throw new FormatException();
            this.Value = value;
        }

        /// <summary>
        /// Converts the current JSON object to a boolean value.
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

        public override T TryGetEnum<T>(T defaultValue = default, bool ignoreCase = false)
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

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteNumberValue(Value);
        }

        public override JObject Clone()
        {
            return this;
        }

        public static implicit operator JNumber(double value)
        {
            return new JNumber(value);
        }
    }
}
