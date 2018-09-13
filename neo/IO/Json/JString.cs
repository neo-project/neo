using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;

namespace Neo.IO.Json
{
    public class JString : JObject
    {
        public string Value { get; private set; }

        public JString(string value)
        {
            if (value == null)
                throw new ArgumentNullException();
            this.Value = value;
        }

        public override bool AsBoolean()
        {
            switch (Value.ToLower())
            {
                case "0":
                case "f":
                case "false":
                case "n":
                case "no":
                case "off":
                    return false;
                default:
                    return true;
            }
        }

        public override T AsEnum<T>(bool ignoreCase = false)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), Value, ignoreCase);
            }
            catch
            {
                throw new InvalidCastException();
            }
        }

        public override double AsNumber()
        {
            try
            {
                return double.Parse(Value);
            }
            catch
            {
                throw new InvalidCastException();
            }
        }

        public override string AsString()
        {
            return Value;
        }

        public override bool CanConvertTo(Type type)
        {
            if (type == typeof(bool))
                return true;
            if (type.GetTypeInfo().IsEnum && Enum.IsDefined(type, Value))
                return true;
            if (type == typeof(double))
                return true;
            if (type == typeof(string))
                return true;
            return false;
        }

        internal static JString Parse(TextReader reader)
        {
            SkipSpace(reader);
            char[] buffer = new char[4];
            char firstChar = (char)reader.Read();
            if (firstChar != '\"' && firstChar != '\'') throw new FormatException();
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = (char)reader.Read();
                if (c == 65535) throw new FormatException();
                if (c == firstChar) break;
                if (c == '\\')
                {
                    c = (char)reader.Read();
                    if (c == 'u')
                    {
                        reader.Read(buffer, 0, 4);
                        c = (char)short.Parse(new string(buffer), NumberStyles.HexNumber);
                    }
                }
                sb.Append(c);
            }
            return new JString(sb.ToString());
        }

        public override string ToString()
        {
            return $"\"{JavaScriptEncoder.Default.Encode(Value)}\"";
        }
    }
}
