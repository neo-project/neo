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
    /// Represents a JSON string.
    /// </summary>
    public class JString : JObject
    {
        /// <summary>
        /// Gets the value of the JSON object.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JString"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value of the JSON object.</param>
        public JString(string value)
        {
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Converts the current JSON object to a boolean value.
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

        public override T TryGetEnum<T>(T defaultValue = default, bool ignoreCase = false)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), Value, ignoreCase);
            }
            catch
            {
                return defaultValue;
            }
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteStringValue(Value);
        }

        public override JObject Clone()
        {
            return this;
        }

        public static implicit operator JString(Enum value)
        {
            return new JString(value.ToString());
        }

        public static implicit operator JString(string value)
        {
            return value == null ? null : new JString(value);
        }
    }
}
