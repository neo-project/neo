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
            return double.TryParse(Value, out double result) ? result : double.NaN;
        }

        public override string AsString()
        {
            return Value;
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
                    switch (c)
                    {
                        case 'u':
                            reader.Read(buffer, 0, 4);
                            c = (char)short.Parse(new string(buffer), NumberStyles.HexNumber);
                            break;
                        case 'r':
                            c = '\r';
                            break;
                        case 'n':
                            c = '\n';
                            break;
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
