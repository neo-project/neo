using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using static Neo.SmartContract.BinarySerializer;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.SmartContract
{
    /// <summary>
    /// A JSON serializer for <see cref="StackItem"/>.
    /// </summary>
    public static class JsonSerializer
    {
        /// <summary>
        /// Serializes a <see cref="StackItem"/> to a <see cref="JObject"/>.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to serialize.</param>
        /// <returns>The serialized object.</returns>
        public static JObject Serialize(StackItem item)
        {
            switch (item)
            {
                case Array array:
                    {
                        return array.Select(p => Serialize(p)).ToArray();
                    }
                case ByteString _:
                case Buffer _:
                    {
                        return item.GetString();
                    }
                case Integer num:
                    {
                        var integer = num.GetInteger();
                        if (integer > JNumber.MAX_SAFE_INTEGER || integer < JNumber.MIN_SAFE_INTEGER)
                            throw new InvalidOperationException();
                        return (double)integer;
                    }
                case Boolean boolean:
                    {
                        return boolean.GetBoolean();
                    }
                case Map map:
                    {
                        var ret = new JObject();

                        foreach (var entry in map)
                        {
                            if (!(entry.Key is ByteString)) throw new FormatException();

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
        /// Serializes a <see cref="StackItem"/> to JSON.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to convert.</param>
        /// <param name="maxSize">The maximum size of the JSON output.</param>
        /// <returns>A byte array containing the JSON output.</returns>
        public static byte[] SerializeToByteArray(StackItem item, uint maxSize)
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms, new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            });
            Stack stack = new();
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
                    case StackItem buffer when buffer is ByteString || buffer is Buffer:
                        writer.WriteStringValue(buffer.GetString());
                        break;
                    case Integer num:
                        {
                            var integer = num.GetInteger();
                            if (integer > JNumber.MAX_SAFE_INTEGER || integer < JNumber.MIN_SAFE_INTEGER)
                                throw new InvalidOperationException();
                            writer.WriteNumberValue((double)integer);
                            break;
                        }
                    case Boolean boolean:
                        writer.WriteBooleanValue(boolean.GetBoolean());
                        break;
                    case Map map:
                        writer.WriteStartObject();
                        stack.Push(JsonTokenType.EndObject);
                        foreach (var pair in map.Reverse())
                        {
                            if (!(pair.Key is ByteString)) throw new FormatException();
                            stack.Push(pair.Value);
                            stack.Push(pair.Key);
                            stack.Push(JsonTokenType.PropertyName);
                        }
                        break;
                    case JsonTokenType.EndObject:
                        writer.WriteEndObject();
                        break;
                    case JsonTokenType.PropertyName:
                        writer.WritePropertyName(((StackItem)stack.Pop()).GetString());
                        break;
                    case Null _:
                        writer.WriteNullValue();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                if (ms.Position + writer.BytesPending > maxSize) throw new InvalidOperationException();
            }
            writer.Flush();
            if (ms.Position > maxSize) throw new InvalidOperationException();
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a <see cref="StackItem"/> from <see cref="JObject"/>.
        /// </summary>
        /// <param name="json">The <see cref="JObject"/> to deserialize.</param>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The deserialized <see cref="StackItem"/>.</returns>
        public static StackItem Deserialize(JObject json, ExecutionEngineLimits limits, ReferenceCounter referenceCounter = null)
        {
            Stack<StackItem> deserialized = new();
            Stack<JObject> stack = new();
            stack.Push(json);
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                switch (item)
                {
                    case null:
                        deserialized.Push(StackItem.Null);
                        break;
                    case JString str:
                        deserialized.Push(str.Value);
                        break;
                    case JNumber num:
                        if ((num.Value % 1) != 0) throw new FormatException("Decimal value is not allowed");
                        deserialized.Push((BigInteger)num.Value);
                        break;
                    case JBoolean boolean:
                        deserialized.Push(new Boolean(boolean.Value));
                        break;
                    case JArray array:
                        deserialized.Push(new ContainerPlaceholder(StackItemType.Array, array.Count));
                        foreach (var value in array.Reverse())
                        {
                            stack.Push(value);
                        }
                        break;
                    case JObject obj:
                        deserialized.Push(new ContainerPlaceholder(StackItemType.Map, obj.Properties.Count));
                        foreach (var entry in obj.Properties.Reverse())
                        {
                            stack.Push(entry.Value);
                            stack.Push((JString)entry.Key);
                        }
                        break;
                    default: throw new FormatException();
                }
                if (deserialized.Count > limits.MaxStackSize) throw new FormatException();
            }

            Stack<StackItem> stack_temp = new();
            while (deserialized.Count > 0)
            {
                StackItem item = deserialized.Pop();
                if (item is ContainerPlaceholder placeholder)
                {
                    switch (placeholder.Type)
                    {
                        case StackItemType.Array:
                            Array array = new(referenceCounter);
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                array.Add(stack_temp.Pop());
                            item = array;
                            break;
                        case StackItemType.Map:
                            Map map = new(referenceCounter);
                            for (int i = 0; i < placeholder.ElementCount; i++)
                            {
                                StackItem key = stack_temp.Pop();
                                StackItem value = stack_temp.Pop();
                                map[(PrimitiveType)key] = value;
                            }
                            item = map;
                            break;
                    }
                }
                stack_temp.Push(item);
            }
            return stack_temp.Peek();
        }
    }
}
