using System;
using System.IO;

namespace Neo.IO.Json
{
    public class JBoolean : JObject
    {
        public bool Value { get; private set; }

        public JBoolean(bool value = false)
        {
            this.Value = value;
        }

        public override bool AsBoolean()
        {
            return Value;
        }

        public override double AsNumber()
        {
            return Value ? 1 : 0;
        }

        public override string AsString()
        {
            return Value.ToString().ToLower();
        }

        internal static JBoolean Parse(TextReader reader)
        {
            SkipSpace(reader);
            char firstChar = (char)reader.Read();
            if (firstChar == 't')
            {
                int c2 = reader.Read();
                int c3 = reader.Read();
                int c4 = reader.Read();
                if (c2 == 'r' && c3 == 'u' && c4 == 'e')
                {
                    return new JBoolean(true);
                }
            }
            else if (firstChar == 'f')
            {
                int c2 = reader.Read();
                int c3 = reader.Read();
                int c4 = reader.Read();
                int c5 = reader.Read();
                if (c2 == 'a' && c3 == 'l' && c4 == 's' && c5 == 'e')
                {
                    return new JBoolean(false);
                }
            }
            throw new FormatException();
        }

        public override string ToString()
        {
            return AsString();
        }
    }
}
