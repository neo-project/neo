using System;
using System.IO;
using System.Text;

namespace Neo.IO.Json
{
    public class JNumber : JObject
    {
        public double Value { get; private set; }

        public JNumber(double value = 0)
        {
            this.Value = value;
        }

        public override bool AsBoolean()
        {
            return Value != 0 && !double.IsNaN(Value);
        }

        public override double AsNumber()
        {
            return Value;
        }

        public override string AsString()
        {
            if (double.IsPositiveInfinity(Value)) return "Infinity";
            if (double.IsNegativeInfinity(Value)) return "-Infinity";
            return Value.ToString();
        }

        internal static JNumber Parse(TextReader reader)
        {
            SkipSpace(reader);
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = (char)reader.Peek();
                if (c >= '0' && c <= '9' || c == '.' || c == '-')
                {
                    sb.Append(c);
                    reader.Read();
                }
                else
                {
                    break;
                }
            }
            return new JNumber(double.Parse(sb.ToString()));
        }

        public override string ToString()
        {
            return AsString();
        }

        public DateTime ToTimestamp()
        {
            if (Value < 0 || Value > ulong.MaxValue)
                throw new InvalidCastException();
            return ((ulong)Value).ToDateTime();
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
    }
}
