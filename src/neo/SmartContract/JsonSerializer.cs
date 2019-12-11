using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
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
            int preExistingSize = 0;
            int consumedBytes;
            return Serialize(item, out consumedBytes, preExistingSize, maxSize);
        }

        /// <summary>
        /// Convert stack item in json
        /// </summary>
        /// <param name="item">Item</param>
        /// <returns>Json</returns>
        public static JObject Serialize(StackItem item, out int consumedBytes, int preExistingSize = 0, int maxSize = -1)
        {
            JObject returnValue;
            switch (item)
            {
                case Array array:
                    {
                        var list = new List<JObject>();
                        foreach (var arrayItem in array)
                        {
                            int usedBytes = 0;
                            list.Add(Serialize(arrayItem, out usedBytes, preExistingSize, maxSize));
                            preExistingSize += usedBytes;
                        }
                        returnValue = list.ToArray();
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
                        {
                            returnValue = integer.ToString();
                        }
                        else
                        {
                            returnValue = (double)num.GetBigInteger();
                        }
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
                            int usedBytes = 0;
                            var key = entry.Key.GetString();
                            var value = Serialize(entry.Value, out usedBytes, preExistingSize, maxSize);
                            ret[key] = value;
                            preExistingSize += usedBytes;
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

            consumedBytes = returnValue.ToByteArray(false).Length;

            if (maxSize != -1 && preExistingSize + consumedBytes > maxSize)
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
