using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract
{
    public static class JsonSerializer
    {
        /// <summary>
        /// Convert stack item in json
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Json</returns>
        public static JObject Serialize(StackItem item, int maxSize = -1)
        {
            JObject returnValue = JObject.Null;
            switch (item)
            {
                case Array array:
                    {
                        returnValue = array.Select(p => Serialize(p, maxSize)).ToArray();
                        break;
                    }
                case ByteArray buffer:
                    {
                        returnValue = Convert.ToBase64String(buffer.GetSpan());
                        break;
                    }
                case Integer num:
                    {
                        var integer = num.GetBigInteger();
                        if (integer > JNumber.MAX_SAFE_INTEGER || integer < JNumber.MIN_SAFE_INTEGER)
                            returnValue = integer.ToString();
                        returnValue = (double)num.GetBigInteger();
                        break;
                    }
                case Boolean boolean:
                    {
                        returnValue = boolean.ToBoolean();
                        break;
                    }
                case Map map:
                    {
                        var ret = new JObject();

                        foreach (var entry in map)
                        {
                            var key = entry.Key.GetString();
                            var value = Serialize(entry.Value, maxSize);

                            ret[key] = value;
                        }

                        returnValue = ret;
                        break;
                    }
                case Null _:
                    {
                        returnValue = JObject.Null;
                        break;
                    }
                default: throw new FormatException();
            }

            if(maxSize != -1 && returnValue.ToByteArray(false).Length > maxSize)
            {
                throw new FormatException();
            }

            return returnValue;
        }

        /// <summary>
        /// Convert json object to stack item
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return stack item</returns>
        public static StackItem Deserialize(JObject json, ReferenceCounter referenceCounter = null)
        {
            switch (json)
            {
                case null:
                    {
                        return StackItem.Null;
                    }
                case JArray array:
                    {
                        return new Array(referenceCounter, array.Select(p => Deserialize(p, referenceCounter)));
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
                        return new Boolean(boolean.Value);
                    }
                case JObject obj:
                    {
                        var item = new Map(referenceCounter);

                        foreach (var entry in obj.Properties)
                        {
                            var key = entry.Key;
                            var value = Deserialize(entry.Value, referenceCounter);

                            item[key] = value;
                        }

                        return item;
                    }
                default: throw new FormatException();
            }
        }
    }
}
