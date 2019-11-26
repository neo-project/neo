using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.IO.Json
{
    public class JObject
    {
        protected const char BEGIN_ARRAY = '[';
        protected const char BEGIN_OBJECT = '{';
        protected const char END_ARRAY = ']';
        protected const char END_OBJECT = '}';
        protected const char NAME_SEPARATOR = ':';
        protected const char VALUE_SEPARATOR = ',';
        protected const char QUOTATION_MARK = '"';
        protected const string WS = " \t\n\r";
        protected const string LITERAL_FALSE = "false";
        protected const string LITERAL_NULL = "null";
        protected const string LITERAL_TRUE = "true";

        public static readonly JObject Null = null;
        public IDictionary<string, JObject> Properties { get; } = new OrderedDictionary<string, JObject>();

        public JObject this[string name]
        {
            get
            {
                Properties.TryGetValue(name, out JObject value);
                return value;
            }
            set
            {
                Properties[name] = value;
            }
        }

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
            return ToString();
        }

        public bool ContainsProperty(string key)
        {
            return Properties.ContainsKey(key);
        }

        public static JObject Parse(TextReader reader, int max_nest = 100)
        {
            if (max_nest < 0) throw new FormatException();
            SkipSpace(reader);
            char firstChar = (char)reader.Peek();
            if (firstChar == LITERAL_FALSE[0])
                return JBoolean.ParseFalse(reader);
            if (firstChar == LITERAL_NULL[0])
                return ParseNull(reader);
            if (firstChar == LITERAL_TRUE[0])
                return JBoolean.ParseTrue(reader);
            if (firstChar == BEGIN_OBJECT)
                return ParseObject(reader, max_nest);
            if (firstChar == BEGIN_ARRAY)
                return JArray.Parse(reader, max_nest);
            if ((firstChar >= '0' && firstChar <= '9') || firstChar == '-')
                return JNumber.Parse(reader);
            if (firstChar == QUOTATION_MARK)
                return JString.Parse(reader);
            throw new FormatException();
        }

        public static JObject Parse(string value, int max_nest = 100)
        {
            using (StringReader reader = new StringReader(value))
            {
                JObject json = Parse(reader, max_nest);
                SkipSpace(reader);
                if (reader.Read() != -1) throw new FormatException();
                return json;
            }
        }

        private static JObject ParseNull(TextReader reader)
        {
            for (int i = 0; i < LITERAL_NULL.Length; i++)
                if ((char)reader.Read() != LITERAL_NULL[i])
                    throw new FormatException();
            return null;
        }

        private static JObject ParseObject(TextReader reader, int max_nest)
        {
            SkipSpace(reader);
            if (reader.Read() != BEGIN_OBJECT) throw new FormatException();
            JObject obj = new JObject();
            SkipSpace(reader);
            if (reader.Peek() != END_OBJECT)
            {
                while (true)
                {
                    string name = JString.Parse(reader).Value;
                    if (obj.Properties.ContainsKey(name)) throw new FormatException();
                    SkipSpace(reader);
                    if (reader.Read() != NAME_SEPARATOR) throw new FormatException();
                    JObject value = Parse(reader, max_nest - 1);
                    obj.Properties.Add(name, value);
                    SkipSpace(reader);
                    char nextchar = (char)reader.Read();
                    if (nextchar == VALUE_SEPARATOR) continue;
                    if (nextchar == END_OBJECT) break;
                    throw new FormatException();
                }
            }
            else
            {
                reader.Read();
            }
            return obj;
        }

        protected static void SkipSpace(TextReader reader)
        {
            while (WS.IndexOf((char)reader.Peek()) >= 0)
            {
                reader.Read();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(BEGIN_OBJECT);
            foreach (KeyValuePair<string, JObject> pair in Properties)
            {
                sb.Append((JObject)pair.Key);
                sb.Append(NAME_SEPARATOR);
                if (pair.Value == null)
                {
                    sb.Append(LITERAL_NULL);
                }
                else
                {
                    sb.Append(pair.Value);
                }
                sb.Append(VALUE_SEPARATOR);
            }
            if (Properties.Count == 0)
            {
                sb.Append(END_OBJECT);
            }
            else
            {
                sb[sb.Length - 1] = END_OBJECT;
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
