// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.VM
{
    /// <summary>
    /// A helper class related to NeoVM.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Emits the opcodes for creating an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the array.</typeparam>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="list">The elements of the array.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder CreateArray<T>(this ScriptBuilder builder, IReadOnlyList<T> list = null)
        {
            if (list is null || list.Count == 0)
                return builder.Emit(OpCode.NEWARRAY0);
            for (int i = list.Count - 1; i >= 0; i--)
                builder.EmitPush(list[i]);
            builder.EmitPush(list.Count);
            return builder.Emit(OpCode.PACK);
        }

        /// <summary>
        /// Emits the opcodes for creating a map.
        /// </summary>
        /// <typeparam name="TKey">The type of the key of the map.</typeparam>
        /// <typeparam name="TValue">The type of the value of the map.</typeparam>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="map">The key/value pairs of the map.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder CreateMap<TKey, TValue>(this ScriptBuilder builder, IEnumerable<KeyValuePair<TKey, TValue>> map = null)
        {
            builder.Emit(OpCode.NEWMAP);
            if (map != null)
                foreach (var p in map)
                {
                    builder.Emit(OpCode.DUP);
                    builder.EmitPush(p.Key);
                    builder.EmitPush(p.Value);
                    builder.Emit(OpCode.SETITEM);
                }
            return builder;
        }

        /// <summary>
        /// Emits the specified opcodes.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="ops">The opcodes to emit.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder Emit(this ScriptBuilder builder, params OpCode[] ops)
        {
            foreach (OpCode op in ops)
                builder.Emit(op);
            return builder;
        }

        /// <summary>
        /// Emits the opcodes for calling a contract dynamically.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="scriptHash">The hash of the contract to be called.</param>
        /// <param name="method">The method to be called in the contract.</param>
        /// <param name="args">The arguments for calling the contract.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder EmitDynamicCall(this ScriptBuilder builder, UInt160 scriptHash, string method, params object[] args)
        {
            return EmitDynamicCall(builder, scriptHash, method, CallFlags.All, args);
        }

        /// <summary>
        /// Emits the opcodes for calling a contract dynamically.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="scriptHash">The hash of the contract to be called.</param>
        /// <param name="method">The method to be called in the contract.</param>
        /// <param name="flags">The <see cref="CallFlags"/> for calling the contract.</param>
        /// <param name="args">The arguments for calling the contract.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder EmitDynamicCall(this ScriptBuilder builder, UInt160 scriptHash, string method, CallFlags flags, params object[] args)
        {
            builder.CreateArray(args);
            builder.EmitPush(flags);
            builder.EmitPush(method);
            builder.EmitPush(scriptHash);
            builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
            return builder;
        }

        /// <summary>
        /// Emits the opcodes for pushing the specified data onto the stack.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="data">The data to be pushed.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder EmitPush(this ScriptBuilder builder, ISerializable data)
        {
            return builder.EmitPush(data.ToArray());
        }

        /// <summary>
        /// Emits the opcodes for pushing the specified data onto the stack.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="parameter">The data to be pushed.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder EmitPush(this ScriptBuilder builder, ContractParameter parameter)
        {
            if (parameter.Value is null)
                builder.Emit(OpCode.PUSHNULL);
            else
                switch (parameter.Type)
                {
                    case ContractParameterType.Signature:
                    case ContractParameterType.ByteArray:
                        builder.EmitPush((byte[])parameter.Value);
                        break;
                    case ContractParameterType.Boolean:
                        builder.EmitPush((bool)parameter.Value);
                        break;
                    case ContractParameterType.Integer:
                        if (parameter.Value is BigInteger bi)
                            builder.EmitPush(bi);
                        else
                            builder.EmitPush((BigInteger)typeof(BigInteger).GetConstructor(new[] { parameter.Value.GetType() }).Invoke(new[] { parameter.Value }));
                        break;
                    case ContractParameterType.Hash160:
                        builder.EmitPush((UInt160)parameter.Value);
                        break;
                    case ContractParameterType.Hash256:
                        builder.EmitPush((UInt256)parameter.Value);
                        break;
                    case ContractParameterType.PublicKey:
                        builder.EmitPush((ECPoint)parameter.Value);
                        break;
                    case ContractParameterType.String:
                        builder.EmitPush((string)parameter.Value);
                        break;
                    case ContractParameterType.Array:
                        {
                            IList<ContractParameter> parameters = (IList<ContractParameter>)parameter.Value;
                            for (int i = parameters.Count - 1; i >= 0; i--)
                                builder.EmitPush(parameters[i]);
                            builder.EmitPush(parameters.Count);
                            builder.Emit(OpCode.PACK);
                        }
                        break;
                    case ContractParameterType.Map:
                        {
                            var pairs = (IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value;
                            builder.CreateMap(pairs);
                        }
                        break;
                    default:
                        throw new ArgumentException(null, nameof(parameter));
                }
            return builder;
        }

        /// <summary>
        /// Emits the opcodes for pushing the specified data onto the stack.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="obj">The data to be pushed.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder EmitPush(this ScriptBuilder builder, object obj)
        {
            switch (obj)
            {
                case bool data:
                    builder.EmitPush(data);
                    break;
                case byte[] data:
                    builder.EmitPush(data);
                    break;
                case string data:
                    builder.EmitPush(data);
                    break;
                case BigInteger data:
                    builder.EmitPush(data);
                    break;
                case ISerializable data:
                    builder.EmitPush(data);
                    break;
                case sbyte data:
                    builder.EmitPush(data);
                    break;
                case byte data:
                    builder.EmitPush(data);
                    break;
                case short data:
                    builder.EmitPush(data);
                    break;
                case ushort data:
                    builder.EmitPush(data);
                    break;
                case int data:
                    builder.EmitPush(data);
                    break;
                case uint data:
                    builder.EmitPush(data);
                    break;
                case long data:
                    builder.EmitPush(data);
                    break;
                case ulong data:
                    builder.EmitPush(data);
                    break;
                case Enum data:
                    builder.EmitPush(BigInteger.Parse(data.ToString("d")));
                    break;
                case ContractParameter data:
                    builder.EmitPush(data);
                    break;
                case null:
                    builder.Emit(OpCode.PUSHNULL);
                    break;
                default:
                    throw new ArgumentException(null, nameof(obj));
            }
            return builder;
        }

        /// <summary>
        /// Emits the opcodes for invoking an interoperable service.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="method">The hash of the interoperable service.</param>
        /// <param name="args">The arguments for calling the interoperable service.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder EmitSysCall(this ScriptBuilder builder, uint method, params object[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                EmitPush(builder, args[i]);
            return builder.EmitSysCall(method);
        }

        /// <summary>
        /// Generates the script for calling a contract dynamically.
        /// </summary>
        /// <param name="scriptHash">The hash of the contract to be called.</param>
        /// <param name="method">The method to be called in the contract.</param>
        /// <param name="args">The arguments for calling the contract.</param>
        /// <returns>The generated script.</returns>
        public static byte[] MakeScript(this UInt160 scriptHash, string method, params object[] args)
        {
            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(scriptHash, method, args);
            return sb.ToArray();
        }

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

        /// <summary>
        /// Converts the <see cref="EvaluationStack"/> to a JSON object.
        /// </summary>
        /// <param name="stack">The <see cref="EvaluationStack"/> to convert.</param>
        /// <param name="maxSize">The maximum size in bytes of the result.</param>
        /// <returns>The <see cref="EvaluationStack"/> represented by a JSON object.</returns>
        public static JArray ToJson(this EvaluationStack stack, int maxSize = int.MaxValue)
        {
            if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize));
            maxSize -= 2/*[]*/+ Math.Max(0, (stack.Count - 1))/*,*/;
            JArray result = new();
            foreach (var item in stack)
                result.Add(ToJson(item, null, ref maxSize));
            if (maxSize < 0) throw new InvalidOperationException("Max size reached.");
            return result;
        }

        private static JObject ToJson(StackItem item, HashSet<StackItem> context, ref int maxSize)
        {
            JObject json = new();
            JObject value = null;
            json["type"] = item.Type;
            maxSize -= 11/*{"type":""}*/+ item.Type.ToString().Length;
            switch (item)
            {
                case Array array:
                    {
                        context ??= new HashSet<StackItem>(ReferenceEqualityComparer.Instance);
                        if (!context.Add(array)) throw new InvalidOperationException();
                        maxSize -= 2/*[]*/+ Math.Max(0, (array.Count - 1))/*,*/;
                        JArray a = new();
                        foreach (StackItem stackItem in array)
                            a.Add(ToJson(stackItem, context, ref maxSize));
                        value = a;
                        break;
                    }
                case Boolean boolean:
                    {
                        bool b = boolean.GetBoolean();
                        maxSize -= b ? 4/*true*/: 5/*false*/;
                        value = b;
                        break;
                    }
                case Buffer _:
                case ByteString _:
                    {
                        string s = Convert.ToBase64String(item.GetSpan());
                        maxSize -= 2/*""*/+ s.Length;
                        value = s;
                        break;
                    }
                case Integer integer:
                    {
                        string s = integer.GetInteger().ToString();
                        maxSize -= 2/*""*/+ s.Length;
                        value = s;
                        break;
                    }
                case Map map:
                    {
                        context ??= new HashSet<StackItem>(ReferenceEqualityComparer.Instance);
                        if (!context.Add(map)) throw new InvalidOperationException();
                        maxSize -= 2/*[]*/+ Math.Max(0, (map.Count - 1))/*,*/;
                        JArray a = new();
                        foreach (var (k, v) in map)
                        {
                            maxSize -= 17/*{"key":,"value":}*/;
                            JObject i = new();
                            i["key"] = ToJson(k, context, ref maxSize);
                            i["value"] = ToJson(v, context, ref maxSize);
                            a.Add(i);
                        }
                        value = a;
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

        private static ContractParameter ToParameter(StackItem item, List<(StackItem, ContractParameter)> context)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            ContractParameter parameter = null;
            switch (item)
            {
                case Array array:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
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
                        context = new List<(StackItem, ContractParameter)>();
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

        /// <summary>
        /// Converts the <see cref="ContractParameter"/> to a <see cref="StackItem"/>.
        /// </summary>
        /// <param name="parameter">The <see cref="ContractParameter"/> to convert.</param>
        /// <returns>The converted <see cref="StackItem"/>.</returns>
        public static StackItem ToStackItem(this ContractParameter parameter)
        {
            return ToStackItem(parameter, null);
        }

        private static StackItem ToStackItem(ContractParameter parameter, List<(StackItem, ContractParameter)> context)
        {
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));
            if (parameter.Value is null) return StackItem.Null;
            StackItem stackItem = null;
            switch (parameter.Type)
            {
                case ContractParameterType.Array:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
                    else
                        (stackItem, _) = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter));
                    if (stackItem is null)
                    {
                        stackItem = new Array(((IList<ContractParameter>)parameter.Value).Select(p => ToStackItem(p, context)));
                        context.Add((stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Map:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
                    else
                        (stackItem, _) = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter));
                    if (stackItem is null)
                    {
                        Map map = new();
                        foreach (var pair in (IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value)
                            map[(PrimitiveType)ToStackItem(pair.Key, context)] = ToStackItem(pair.Value, context);
                        stackItem = map;
                        context.Add((stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Boolean:
                    stackItem = (bool)parameter.Value;
                    break;
                case ContractParameterType.ByteArray:
                case ContractParameterType.Signature:
                    stackItem = (byte[])parameter.Value;
                    break;
                case ContractParameterType.Integer:
                    stackItem = (BigInteger)parameter.Value;
                    break;
                case ContractParameterType.Hash160:
                    stackItem = ((UInt160)parameter.Value).ToArray();
                    break;
                case ContractParameterType.Hash256:
                    stackItem = ((UInt256)parameter.Value).ToArray();
                    break;
                case ContractParameterType.PublicKey:
                    stackItem = ((ECPoint)parameter.Value).EncodePoint(true);
                    break;
                case ContractParameterType.String:
                    stackItem = (string)parameter.Value;
                    break;
                default:
                    throw new ArgumentException($"ContractParameterType({parameter.Type}) is not supported to StackItem.");
            }
            return stackItem;
        }
    }
}
