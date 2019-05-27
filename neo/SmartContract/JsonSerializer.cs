using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;
using System.Text;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract
{
    public static class JsonSerializer
    {
        private static readonly BigInteger MaxInteger = BigInteger.Pow(2, 53) - 1;

        /// <summary>
        /// Convert stack item in json
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Json</returns>
        public static JObject Serialize(StackItem item)
        {
            switch (item)
            {
                case VMArray array:
                    {
                        var ret = new JArray();

                        foreach (var entry in array)
                        {
                            ret.Add(Serialize(entry));
                        }

                        return ret;
                    }
                case ByteArray buffer:
                    {
                        return new JString(buffer.GetString());
                    }
                case Integer num:
                    {
                        var integer = num.GetBigInteger();
                        if (integer > MaxInteger) return new JString(integer.ToString());

                        return new JNumber((double)num.GetBigInteger());
                    }
                case VMBoolean boolean:
                    {
                        return new JBoolean(boolean.GetBoolean());
                    }
                case Map obj:
                    {
                        var ret = new JObject();

                        foreach (var entry in obj)
                        {
                            var key = entry.Key.GetString();
                            var value = Serialize(entry.Value);

                            ret[key] = value;
                        }

                        return ret;
                    }
                default: throw new FormatException();
            }
        }

        /// <summary>
        /// Convert json object to stack item
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return stack item</returns>
        public static StackItem Deserialize(JObject json)
        {
            switch (json)
            {
                case JArray array:
                    {
                        var item = new VMArray();

                        foreach (var entry in array)
                        {
                            item.Add(Deserialize(entry));
                        }

                        return item;
                    }
                case JString str:
                    {
                        return new ByteArray(Encoding.UTF8.GetBytes(str.Value));
                    }
                case JNumber num:
                    {
                        if ((num.Value % 1) != 0) throw new FormatException("Decimal value is not allowed");

                        return new Integer((BigInteger)num.Value);
                    }
                case JBoolean boolean:
                    {
                        return new VMBoolean(boolean.Value);
                    }
                case JObject obj:
                    {
                        var item = new Map();

                        foreach (var entry in obj.Properties)
                        {
                            var key = new ByteArray(Encoding.UTF8.GetBytes(entry.Key));
                            var value = Deserialize(entry.Value);

                            item.Add(key, value);
                        }

                        return item;
                    }
                default: throw new FormatException();
            }
        }
    }
}
