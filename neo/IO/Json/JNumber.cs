using System;
using System.IO;
using System.Reflection;
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
            if (Value == 0)
                return false;
            return true;
        }

        public override T AsEnum<T>(bool ignoreCase = false)
        {
            Type t = typeof(T);
            TypeInfo ti = t.GetTypeInfo();
            if (!ti.IsEnum)
                throw new InvalidCastException();
            if (ti.GetEnumUnderlyingType() == typeof(byte))
                return (T)Enum.ToObject(t, (byte)Value);
            if (ti.GetEnumUnderlyingType() == typeof(int))
                return (T)Enum.ToObject(t, (int)Value);
            if (ti.GetEnumUnderlyingType() == typeof(long))
                return (T)Enum.ToObject(t, (long)Value);
            if (ti.GetEnumUnderlyingType() == typeof(sbyte))
                return (T)Enum.ToObject(t, (sbyte)Value);
            if (ti.GetEnumUnderlyingType() == typeof(short))
                return (T)Enum.ToObject(t, (short)Value);
            if (ti.GetEnumUnderlyingType() == typeof(uint))
                return (T)Enum.ToObject(t, (uint)Value);
            if (ti.GetEnumUnderlyingType() == typeof(ulong))
                return (T)Enum.ToObject(t, (ulong)Value);
            if (ti.GetEnumUnderlyingType() == typeof(ushort))
                return (T)Enum.ToObject(t, (ushort)Value);
            throw new InvalidCastException();
        }

        public override double AsNumber()
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
            if (type == typeof(double))
                return true;
            if (type == typeof(string))
                return true;
            TypeInfo ti = type.GetTypeInfo();
            if (ti.IsEnum && Enum.IsDefined(type, Convert.ChangeType(Value, ti.GetEnumUnderlyingType())))
                return true;
            return false;
        }

        internal static JNumber Parse(TextReader reader)
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
            return new JNumber(double.Parse(sb.ToString()));
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
