using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract
{
    public static class JsonSerializer
    {
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
                        return array.Select(p => Serialize(p)).ToArray();
                    }
                case ByteArray buffer:
                    {
                        return buffer.GetByteArray().ToHexString();
                    }
                case Integer num:
                    {
                        var integer = num.GetBigInteger();
                        if (integer > JNumber.MAX_SAFE_INTEGER || integer < JNumber.MIN_SAFE_INTEGER)
                            return integer.ToString();
                        return (double)num.GetBigInteger();
                    }
                case VMBoolean boolean:
                    {
                        return boolean.GetBoolean();
                    }
                case Map map:
                    {
                        var ret = new JObject();

                        foreach (var entry in map)
                        {
                            var key = entry.Key.GetString();
                            var value = Serialize(entry.Value);

                            ret[key] = value;
                        }

                        return ret;
                    }
                case Null _:
                    {
                        return JObject.Null;
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
                case null:
                    {
                        return StackItem.Null;
                    }
                case JArray array:
                    {
                        return array.Select(p => Deserialize(p)).ToList();
                    }
                case JString str:
                    {
                        return str.Value;
                    }
                case JNumber num:
                    {
                        if ((num.Value % 1) != 0) throw new FormatException("Decimal value is not allowed");

                        return (BigInteger)num.Value;
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
                            var key = entry.Key;
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
