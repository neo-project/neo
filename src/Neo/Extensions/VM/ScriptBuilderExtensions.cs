// Copyright (C) 2015-2025 The Neo Project.
//
// ScriptBuilderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.Extensions
{
    public static class ScriptBuilderExtensions
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
            for (var i = list.Count - 1; i >= 0; i--)
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
        public static ScriptBuilder CreateMap<TKey, TValue>(this ScriptBuilder builder, IEnumerable<KeyValuePair<TKey, TValue>> map)
            where TKey : notnull
            where TValue : notnull
        {
            var count = map.Count();

            if (count == 0)
                return builder.Emit(OpCode.NEWMAP);

            foreach (var (key, value) in map.Reverse())
            {
                builder.EmitPush(value);
                builder.EmitPush(key);
            }
            builder.EmitPush(count);
            return builder.Emit(OpCode.PACKMAP);
        }

        /// <summary>
        /// Emits the opcodes for creating a map.
        /// </summary>
        /// <typeparam name="TKey">The type of the key of the map.</typeparam>
        /// <typeparam name="TValue">The type of the value of the map.</typeparam>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="map">The key/value pairs of the map.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder CreateMap<TKey, TValue>(this ScriptBuilder builder, IReadOnlyDictionary<TKey, TValue> map)
            where TKey : notnull
            where TValue : notnull
        {
            if (map.Count == 0)
                return builder.Emit(OpCode.NEWMAP);

            foreach (var (key, value) in map.Reverse())
            {
                builder.EmitPush(value);
                builder.EmitPush(key);
            }
            builder.EmitPush(map.Count);
            return builder.Emit(OpCode.PACKMAP);
        }

        /// <summary>
        /// Emits the opcodes for creating a struct.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="array">The list of properties.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder CreateStruct<T>(this ScriptBuilder builder, IReadOnlyList<T> array)
            where T : notnull
        {
            if (array.Count == 0)
                return builder.Emit(OpCode.NEWSTRUCT0);
            for (var i = array.Count - 1; i >= 0; i--)
                builder.EmitPush(array[i]);
            builder.EmitPush(array.Count);
            return builder.Emit(OpCode.PACKSTRUCT);
        }

        /// <summary>
        /// Emits the specified opcodes.
        /// </summary>
        /// <param name="builder">The <see cref="ScriptBuilder"/> to be used.</param>
        /// <param name="ops">The opcodes to emit.</param>
        /// <returns>The same instance as <paramref name="builder"/>.</returns>
        public static ScriptBuilder Emit(this ScriptBuilder builder, params OpCode[] ops)
        {
            foreach (var op in ops)
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
                            builder.EmitPush((BigInteger)typeof(BigInteger).GetConstructor([parameter.Value.GetType()]).Invoke([parameter.Value]));
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
                            var parameters = (IList<ContractParameter>)parameter.Value;
                            for (var i = parameters.Count - 1; i >= 0; i--)
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
                case char data:
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
            for (var i = args.Length - 1; i >= 0; i--)
                EmitPush(builder, args[i]);
            return builder.EmitSysCall(method);
        }
    }
}
