using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;

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

        internal static JString Parse(TextReader reader)
        {
            SkipSpace(reader);
            if (reader.Read() != QUOTATION_MARK) throw new FormatException();
            char[] buffer = new char[4];
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int c = reader.Read();
                if (c == QUOTATION_MARK) break;
                if (c == '\\')
                {
                    c = (char)reader.Read();
                    switch (c)
                    {
                        case QUOTATION_MARK: c = QUOTATION_MARK; break;
                        case '\\': c = '\\'; break;
                        case '/': c = '/'; break;
                        case 'b': c = '\b'; break;
                        case 'f': c = '\f'; break;
                        case 'n': c = '\n'; break;
                        case 'r': c = '\r'; break;
                        case 't': c = '\t'; break;
                        case 'u':
                            reader.Read(buffer, 0, buffer.Length);
                            c = short.Parse(new string(buffer), NumberStyles.HexNumber);
                            break;
                        default: throw new FormatException();
                    }
                }
                else if (c < ' ' || c == -1)
                {
                    throw new FormatException();
                }
                sb.Append((char)c);
            }
            return new JString(sb.ToString());
        }

        public override string ToString()
        {
            return $"{QUOTATION_MARK}{JavaScriptEncoder.Default.Encode(Value)}{QUOTATION_MARK}";
        }

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
    }
}
