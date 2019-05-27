using System;
using System.Globalization;
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
            if (double.IsPositiveInfinity(Value)) throw new FormatException("Positive infinity number");
            if (double.IsNegativeInfinity(Value)) throw new FormatException("Negative infinity number");
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        internal static JNumber Parse(TextReader reader)
        {
            SkipSpace(reader);
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = (char)reader.Peek();

                if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                    reader.Read();
                }
                else if (c == '+' || c == '-')
                {
                    if (sb.Length > 0) throw new FormatException("+ or - only could be the first character");

                    sb.Append(c);
                    reader.Read();
                }
                else if (c == '.')
                {
                    if (sb.Length == 0) throw new FormatException(". could not be the first character");
                    if (sb.ToString().Contains(".")) throw new FormatException("Only one decimal separator is allowed");

                    sb.Append(c);
                    reader.Read();
                }
                else break;
            }

            if (sb.ToString().EndsWith(".")) throw new FormatException(". could not be the last character");

            return new JNumber(double.Parse(sb.ToString(), CultureInfo.InvariantCulture));
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
