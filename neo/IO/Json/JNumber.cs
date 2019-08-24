using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Neo.IO.Json
{
    public class JNumber : JObject
    {
        public static readonly long MAX_SAFE_INTEGER = (long)Math.Pow(2, 53) - 1;
        public static readonly long MIN_SAFE_INTEGER = -MAX_SAFE_INTEGER;

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
            char nextchar = (char)reader.Read();
            if (nextchar == '-')
            {
                sb.Append(nextchar);
                nextchar = (char)reader.Read();
            }
            if (nextchar < '0' || nextchar > '9') throw new FormatException();
            sb.Append(nextchar);
            if (nextchar > '0')
            {
                while (true)
                {
                    char c = (char)reader.Peek();
                    if (c < '0' || c > '9') break;
                    sb.Append((char)reader.Read());
                }
            }
            nextchar = (char)reader.Peek();
            if (nextchar == '.')
            {
                sb.Append((char)reader.Read());
                nextchar = (char)reader.Read();
                if (nextchar < '0' || nextchar > '9') throw new FormatException();
                sb.Append(nextchar);
                while (true)
                {
                    nextchar = (char)reader.Peek();
                    if (nextchar < '0' || nextchar > '9') break;
                    sb.Append((char)reader.Read());
                }
            }
            if (nextchar == 'e' || nextchar == 'E')
            {
                sb.Append((char)reader.Read());
                nextchar = (char)reader.Read();
                if (nextchar == '-' || nextchar == '+')
                {
                    sb.Append(nextchar);
                    nextchar = (char)reader.Read();
                }
                if (nextchar < '0' || nextchar > '9') throw new FormatException();
                sb.Append(nextchar);
                while (true)
                {
                    nextchar = (char)reader.Peek();
                    if (nextchar < '0' || nextchar > '9') break;
                    sb.Append((char)reader.Read());
                }
            }

            var value = double.Parse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            if (value > MAX_SAFE_INTEGER || value < MIN_SAFE_INTEGER)
                throw new FormatException();

            return new JNumber(value);
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
    }
}
