using System;
using System.Globalization;
using System.Text.Json;

namespace Neo.IO.Json
{
    public class JNumber : JObject
    {
        public static readonly long MAX_SAFE_INTEGER = (long)Math.Pow(2, 53) - 1;
        public static readonly long MIN_SAFE_INTEGER = -MAX_SAFE_INTEGER;

        public double Value { get; private set; }

        public JNumber(double value = 0)
        {
            if (!double.IsFinite(value)) throw new FormatException();
            this.Value = value;
        }

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
