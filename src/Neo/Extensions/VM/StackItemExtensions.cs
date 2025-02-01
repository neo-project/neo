// Copyright (C) 2015-2025 The Neo Project.
//
// StackItemExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Extensions
{
    public static class StackItemExtensions
    {
        /// <summary>
        /// Converts the <see cref="StackItem"/> to a JSON object.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to convert.</param>
        /// <param name="maxSize">The maximum size in bytes of the result.</param>
        /// <returns>The <see cref="StackItem"/> represented by a JSON object.</returns>
        public static JObject ToJson(this StackItem item, int maxSize = int.MaxValue)
        {
            return ToJson(item, null, ref maxSize);
        }

        public static JObject ToJson(this StackItem item, HashSet<StackItem> context, ref int maxSize)
        {
            JObject json = new()
            {
                ["type"] = item.Type
            };
            JToken value = null;
            maxSize -= 11/*{"type":""}*/+ item.Type.ToString().Length;
            switch (item)
            {
                case Array array:
                    {
                        context ??= new(ReferenceEqualityComparer.Instance);
                        if (!context.Add(array)) throw new InvalidOperationException("Circular reference.");
                        maxSize -= 2/*[]*/+ Math.Max(0, (array.Count - 1))/*,*/;
                        JArray a = [];
                        foreach (var stackItem in array)
                            a.Add(ToJson(stackItem, context, ref maxSize));
                        value = a;
                        if (!context.Remove(array)) throw new InvalidOperationException("Circular reference.");
                        break;
                    }
                case Boolean boolean:
                    {
                        var b = boolean.GetBoolean();
                        maxSize -= b ? 4/*true*/: 5/*false*/;
                        value = b;
                        break;
                    }
                case Buffer _:
                case ByteString _:
                    {
                        var s = Convert.ToBase64String(item.GetSpan());
                        maxSize -= 2/*""*/+ s.Length;
                        value = s;
                        break;
                    }
                case Integer integer:
                    {
                        var s = integer.GetInteger().ToString();
                        maxSize -= 2/*""*/+ s.Length;
                        value = s;
                        break;
                    }
                case Map map:
                    {
                        context ??= new(ReferenceEqualityComparer.Instance);
                        if (!context.Add(map)) throw new InvalidOperationException("Circular reference.");
                        maxSize -= 2/*[]*/+ Math.Max(0, (map.Count - 1))/*,*/;
                        JArray a = new();
                        foreach (var (k, v) in map)
                        {
                            maxSize -= 17/*{"key":,"value":}*/;
                            JObject i = new()
                            {
                                ["key"] = ToJson(k, context, ref maxSize),
                                ["value"] = ToJson(v, context, ref maxSize)
                            };
                            a.Add(i);
                        }
                        value = a;
                        if (!context.Remove(map)) throw new InvalidOperationException("Circular reference.");
                        break;
                    }
                case Pointer pointer:
                    {
                        maxSize -= pointer.Position.ToString().Length;
                        value = pointer.Position;
                        break;
                    }
            }
            if (value is not null)
            {
                maxSize -= 9/*,"value":*/;
                json["value"] = value;
            }
            if (maxSize < 0) throw new InvalidOperationException("Max size reached.");
            return json;
        }

        /// <summary>
        /// Converts the <see cref="StackItem"/> to a <see cref="ContractParameter"/>.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to convert.</param>
        /// <returns>The converted <see cref="ContractParameter"/>.</returns>
        public static ContractParameter ToParameter(this StackItem item)
        {
            return ToParameter(item, null);
        }

        public static ContractParameter ToParameter(this StackItem item, List<(StackItem, ContractParameter)> context)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            ContractParameter parameter = null;
            switch (item)
            {
                case Array array:
                    if (context is null)
                        context = [];
                    else
                        (_, parameter) = context.FirstOrDefault(p => ReferenceEquals(p.Item1, item));
                    if (parameter is null)
                    {
                        parameter = new ContractParameter { Type = ContractParameterType.Array };
                        context.Add((item, parameter));
                        parameter.Value = array.Select(p => ToParameter(p, context)).ToList();
                    }
                    break;
                case Map map:
                    if (context is null)
                        context = [];
                    else
                        (_, parameter) = context.FirstOrDefault(p => ReferenceEquals(p.Item1, item));
                    if (parameter is null)
                    {
                        parameter = new ContractParameter { Type = ContractParameterType.Map };
                        context.Add((item, parameter));
                        parameter.Value = map.Select(p => new KeyValuePair<ContractParameter, ContractParameter>(ToParameter(p.Key, context), ToParameter(p.Value, context))).ToList();
                    }
                    break;
                case Boolean _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Boolean,
                        Value = item.GetBoolean()
                    };
                    break;
                case ByteString array:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.ByteArray,
                        Value = array.GetSpan().ToArray()
                    };
                    break;
                case Integer i:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Integer,
                        Value = i.GetInteger()
                    };
                    break;
                case InteropInterface _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.InteropInterface
                    };
                    break;
                case Null _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Any
                    };
                    break;
                default:
                    throw new ArgumentException($"StackItemType({item.Type}) is not supported to ContractParameter.");
            }
            return parameter;
        }
    }
}
