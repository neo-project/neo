using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.IO.Json
{
    public class JObject
    {
        public static readonly JObject Null = null;
        private Dictionary<string, JObject> properties = new Dictionary<string, JObject>();

        public JObject this[string name]
        {
            get
            {
                properties.TryGetValue(name, out JObject value);
                return value;
            }
            set
            {
                properties[name] = value;
            }
        }

        public IReadOnlyDictionary<string, JObject> Properties => properties;

        public virtual bool AsBoolean()
        {
            return true;
        }

        public virtual double AsNumber()
        {
            return double.NaN;
        }

        public virtual string AsString()
        {
            return "[object Object]";
        }

        public bool ContainsProperty(string key)
        {
            return properties.ContainsKey(key);
        }

        public static JObject Parse(TextReader reader, int max_nest = 100)
        {
            if (max_nest < 0) throw new FormatException();
            SkipSpace(reader);
            char firstChar = (char)reader.Peek();
            if (firstChar == '\"' || firstChar == '\'')
            {
                return JString.Parse(reader);
            }
            if (firstChar == '[')
            {
                return JArray.Parse(reader, max_nest);
            }
            if ((firstChar >= '0' && firstChar <= '9') || firstChar == '-')
            {
                return JNumber.Parse(reader);
            }
            if (firstChar == 't' || firstChar == 'f')
            {
                return JBoolean.Parse(reader);
            }
            if (firstChar == 'n')
            {
                return ParseNull(reader);
            }
            if (reader.Read() != '{') throw new FormatException();
            SkipSpace(reader);
            JObject obj = new JObject();
            while (reader.Peek() != '}')
            {
                if (reader.Peek() == ',') reader.Read();
                SkipSpace(reader);
                string name = JString.Parse(reader).Value;
                SkipSpace(reader);
                if (reader.Read() != ':') throw new FormatException();
                JObject value = Parse(reader, max_nest - 1);
                obj.properties.Add(name, value);
                SkipSpace(reader);
            }
            reader.Read();
            return obj;
        }

        public static JObject Parse(string value, int max_nest = 100)
        {
            using (StringReader reader = new StringReader(value))
            {
                return Parse(reader, max_nest);
            }
        }

        private static JObject ParseNull(TextReader reader)
        {
            char firstChar = (char)reader.Read();
            if (firstChar == 'n')
            {
                int c2 = reader.Read();
                int c3 = reader.Read();
                int c4 = reader.Read();
                if (c2 == 'u' && c3 == 'l' && c4 == 'l')
                {
                    return null;
                }
            }
            throw new FormatException();
        }

        protected static void SkipSpace(TextReader reader)
        {
            while (reader.Peek() == ' ' || reader.Peek() == '\t' || reader.Peek() == '\r' || reader.Peek() == '\n')
            {
                reader.Read();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            foreach (KeyValuePair<string, JObject> pair in properties)
            {
                sb.Append('"');
                sb.Append(pair.Key);
                sb.Append('"');
                sb.Append(':');
                if (pair.Value == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append(pair.Value);
                }
                sb.Append(',');
            }
            if (properties.Count == 0)
            {
                sb.Append('}');
            }
            else
            {
                sb[sb.Length - 1] = '}';
            }
            return sb.ToString();
        }

        public virtual T TryGetEnum<T>(T defaultValue = default, bool ignoreCase = false) where T : Enum
        {
            return defaultValue;
        }

        public static implicit operator JObject(Enum value)
        {
            return new JString(value.ToString());
        }

        public static implicit operator JObject(JObject[] value)
        {
            return new JArray(value);
        }

        public static implicit operator JObject(bool value)
        {
            return new JBoolean(value);
        }

        public static implicit operator JObject(double value)
        {
            return new JNumber(value);
        }

        public static implicit operator JObject(string value)
        {
            return value == null ? null : new JString(value);
        }
    }
}
