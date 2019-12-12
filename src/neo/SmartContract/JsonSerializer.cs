using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
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
        public static JObject Serialize(StackItem item)
        {
            switch (item)
            {
                case Array array:
                    {
                        return array.Select(p => Serialize(p)).ToArray();
                    }
                case ByteArray buffer:
                    {
                        return Convert.ToBase64String(buffer.GetSpan());
                    }
                case Integer num:
                    {
                        var integer = num.GetBigInteger();
                        if (integer > JNumber.MAX_SAFE_INTEGER || integer < JNumber.MIN_SAFE_INTEGER)
                            return integer.ToString();
                        return (double)num.GetBigInteger();
                    }
                case Boolean boolean:
                    {
                        return boolean.ToBoolean();
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

        public static byte[] SerializeToByteArray(StackItem item, uint maxSize)
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms, new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            });
            Stack stack = new Stack();
            stack.Push(item);
            while (stack.Count > 0)
            {
                switch (stack.Pop())
                {
                    case Array array:
                        writer.WriteStartArray();
                        stack.Push(JsonTokenType.EndArray);
                        for (int i = array.Count - 1; i >= 0; i--)
                            stack.Push(array[i]);
                        break;
                    case JsonTokenType.EndArray:
                        writer.WriteEndArray();
                        break;
                    case ByteArray buffer:
                        writer.WriteStringValue(Convert.ToBase64String(buffer.GetSpan()));
                        break;
                    case Integer num:
                        {
                            var integer = num.GetBigInteger();
                            if (integer > JNumber.MAX_SAFE_INTEGER || integer < JNumber.MIN_SAFE_INTEGER)
                                throw new InvalidOperationException();
                            writer.WriteNumberValue((double)num.GetBigInteger());
                            break;
                        }
                    case Boolean boolean:
                        writer.WriteBooleanValue(boolean.ToBoolean());
                        break;
                    case Map map:
                        writer.WriteStartObject();
                        stack.Push(JsonTokenType.EndObject);
                        foreach (var pair in map.Reverse())
                        {
                            stack.Push(pair.Value);
                            stack.Push(pair.Key);
                            stack.Push(JsonTokenType.PropertyName);
                        }
                        break;
                    case JsonTokenType.EndObject:
                        writer.WriteEndObject();
                        break;
                    case JsonTokenType.PropertyName:
                        writer.WritePropertyName(((PrimitiveType)stack.Pop()).GetSpan());
                        break;
                    case Null _:
                        writer.WriteNullValue();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                if (ms.Position > maxSize) throw new InvalidOperationException();
            }
            writer.Flush();
            if (ms.Position > maxSize) throw new InvalidOperationException();
            return ms.ToArray();
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
