using Neo.IO.Json;
using System;
using System.Numerics;
using System.Text;

namespace Neo.VM
{
    public static class JsonParser
    {
        private static readonly BigInteger MaxInteger = new BigInteger(Math.Pow(2, 53)) - 1;

        /// <summary>
        /// Convert stack item in json
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Json</returns>
        public static JObject Serialize(StackItem item)
        {
            switch (item)
            {
                case Types.Array array:
                    {
                        var ret = new JArray();

                        foreach (var entry in array)
                        {
                            ret.Add(Serialize(entry));
                        }

                        return ret;
                    }
                case Types.ByteArray buffer:
                    {
                        return new JString(buffer.GetString());
                    }
                case Types.Integer num:
                    {
                        var integer = num.GetBigInteger();
                        if (integer > MaxInteger) return new JString(integer.ToString());

                        return new JNumber((double)num.GetBigInteger());
                    }
                case Types.Boolean boolean:
                    {
                        return new JBoolean(boolean.GetBoolean());
                    }
                case Types.Map obj:
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
                        var item = new Types.Array();

                        foreach (var entry in array)
                        {
                            item.Add(Deserialize(entry));
                        }

                        return item;
                    }
                case JString str:
                    {
                        return new Types.ByteArray(Encoding.UTF8.GetBytes(str.Value));
                    }
                case JNumber num:
                    {
                        return new Types.Integer((BigInteger)num.Value);
                    }
                case JBoolean boolean:
                    {
                        return new Types.Boolean(boolean.Value);
                    }
                case JObject obj:
                    {
                        var item = new Types.Map();

                        foreach (var entry in obj.Properties)
                        {
                            var key = new Types.ByteArray(Encoding.UTF8.GetBytes(entry.Key));
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