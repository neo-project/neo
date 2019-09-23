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
            return Value.ToString().ToLowerInvariant();
        }

        internal static JBoolean Parse(TextReader reader)
        {
            SkipSpace(reader);
            char firstChar = (char)reader.Peek();
            if (firstChar == LITERAL_FALSE[0])
                return ParseFalse(reader);
            else if (firstChar == LITERAL_TRUE[0])
                return ParseTrue(reader);
            throw new FormatException();
        }

        internal static JBoolean ParseFalse(TextReader reader)
        {
            SkipSpace(reader);
            for (int i = 0; i < LITERAL_FALSE.Length; i++)
                if ((char)reader.Read() != LITERAL_FALSE[i])
                    throw new FormatException();
            return new JBoolean(false);
        }

        internal static JBoolean ParseTrue(TextReader reader)
        {
            SkipSpace(reader);
            for (int i = 0; i < LITERAL_TRUE.Length; i++)
                if ((char)reader.Read() != LITERAL_TRUE[i])
                    throw new FormatException();
            return new JBoolean(true);
        }

        public override string ToString()
        {
            return AsString();
        }
    }
}
