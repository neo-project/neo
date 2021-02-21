using System;
using System.Globalization;
using System.Text.Json;

namespace Neo.IO.Json
{
    public class JString : JObject
    {
        public string Value { get; private set; }

        public JString(string value)
        {
            this.Value = value ?? throw new ArgumentNullException();
        }

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
