// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Neo.Json;
using Neo.VM;
using Neo.VM.Types;
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
        /// Serializes a <see cref="StackItem"/> to a <see cref="JToken"/>.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to serialize.</param>
        /// <returns>The serialized object.</returns>
        public static JToken Serialize(StackItem item)
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
                        return JToken.Null;
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
        /// Deserializes a <see cref="StackItem"/> from <see cref="JToken"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ApplicationEngine"/> used.</param>
        /// <param name="json">The <see cref="JToken"/> to deserialize.</param>
        /// <param name="limits">The limits for the deserialization.</param>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The deserialized <see cref="StackItem"/>.</returns>
        public static StackItem Deserialize(ApplicationEngine engine, JToken json, ExecutionEngineLimits limits, ReferenceCounter referenceCounter = null)
        {
            uint maxStackSize = limits.MaxStackSize;
            return Deserialize(engine, json, ref maxStackSize, referenceCounter);
        }

        private static StackItem Deserialize(ApplicationEngine engine, JToken json, ref uint maxStackSize, ReferenceCounter referenceCounter)
        {
            if (maxStackSize-- == 0) throw new FormatException();
            switch (json)
            {
                case null:
                    {
                        return StackItem.Null;
                    }
                case JArray array:
                    {
                        List<StackItem> list = new(array.Count);
                        foreach (JToken obj in array)
                            list.Add(Deserialize(engine, obj, ref maxStackSize, referenceCounter));
                        return new Array(referenceCounter, list);
                    }
                case JString str:
                    {
                        return str.Value;
                    }
                case JNumber num:
                    {
                        if ((num.Value % 1) != 0) throw new FormatException("Decimal value is not allowed");
                        if (engine.IsHardforkEnabled(Hardfork.HF_Basilisk))
                        {
                            return BigInteger.Parse(num.Value.ToString(CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
                        }
                        return (BigInteger)num.Value;
                    }
                case JBoolean boolean:
                    {
                        return boolean.Value ? StackItem.True : StackItem.False;
                    }
                case JObject obj:
                    {
                        var item = new Map(referenceCounter);

                        foreach (var entry in obj.Properties)
                        {
                            if (maxStackSize-- == 0) throw new FormatException();

                            var key = entry.Key;
                            var value = Deserialize(engine, entry.Value, ref maxStackSize, referenceCounter);

                            item[key] = value;
                        }

                        return item;
                    }
                default: throw new FormatException();
            }
        }
    }
}
