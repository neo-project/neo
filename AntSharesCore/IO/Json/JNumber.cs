using System;
using System.IO;
using System.Text;

namespace AntShares.IO.Json
{
    public class JNumber : JObject
    {
        public decimal Value { get; private set; }

        public JNumber(decimal value = 0)
        {
            this.Value = value;
        }

        public override bool AsBoolean()
        {
            if (Value == 0)
                return false;
            return true;
        }

        public override T AsEnum<T>(bool ignoreCase = false)
        {
            Type t = typeof(T);
            if (!t.IsEnum)
                throw new InvalidCastException();
            if (t.GetEnumUnderlyingType() == typeof(byte))
                return (T)Enum.ToObject(t, (byte)Value);
            if (t.GetEnumUnderlyingType() == typeof(int))
                return (T)Enum.ToObject(t, (int)Value);
            if (t.GetEnumUnderlyingType() == typeof(long))
                return (T)Enum.ToObject(t, (long)Value);
            if (t.GetEnumUnderlyingType() == typeof(sbyte))
                return (T)Enum.ToObject(t, (sbyte)Value);
            if (t.GetEnumUnderlyingType() == typeof(short))
                return (T)Enum.ToObject(t, (short)Value);
            if (t.GetEnumUnderlyingType() == typeof(uint))
                return (T)Enum.ToObject(t, (uint)Value);
            if (t.GetEnumUnderlyingType() == typeof(ulong))
                return (T)Enum.ToObject(t, (ulong)Value);
            if (t.GetEnumUnderlyingType() == typeof(ushort))
                return (T)Enum.ToObject(t, (ushort)Value);
            throw new InvalidCastException();
        }

        public override decimal AsNumber()
        {
            return Value;
        }

        public override string AsString()
        {
            return Value.ToString();
        }

        public override bool CanConvertTo(Type type)
        {
            if (type == typeof(bool))
                return true;
            if (type.IsEnum && Enum.IsDefined(type, Convert.ChangeType(Value, type.GetEnumUnderlyingType())))
                return true;
            if (type == typeof(decimal))
                return true;
            if (type == typeof(string))
                return true;
            return false;
        }

        internal new static JNumber Parse(TextReader reader)
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
            return new JNumber(decimal.Parse(sb.ToString()));
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public DateTime ToTimestamp()
        {
            if (Value < 0 || Value > ulong.MaxValue)
                throw new InvalidCastException();
            return ((ulong)Value).ToDateTime();
        }
    }
}
