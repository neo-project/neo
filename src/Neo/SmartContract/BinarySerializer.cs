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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.SmartContract
{
    /// <summary>
    /// A binary serializer for <see cref="StackItem"/>.
    /// </summary>
    public static class BinarySerializer
    {
        private class ContainerPlaceholder : StackItem
        {
            public override StackItemType Type { get; }
            public int ElementCount { get; }

            public ContainerPlaceholder(StackItemType type, int count)
            {
                Type = type;
                ElementCount = count;
            }

            public override bool Equals(StackItem other) => throw new NotSupportedException();

            public override int GetHashCode() => throw new NotSupportedException();

            public override bool GetBoolean() => throw new NotSupportedException();
        }

        /// <summary>
        /// Deserializes a <see cref="StackItem"/> from byte array.
        /// </summary>
        /// <param name="data">The byte array to parse.</param>
        /// <param name="limits">The limits for the deserialization.</param>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The deserialized <see cref="StackItem"/>.</returns>
        public static StackItem Deserialize(ReadOnlyMemory<byte> data, ExecutionEngineLimits limits, ReferenceCounter referenceCounter = null)
        {
            MemoryReader reader = new(data);
            return Deserialize(ref reader, (uint)Math.Min(data.Length, limits.MaxItemSize), limits.MaxStackSize, referenceCounter);
        }

        /// <summary>
        /// Deserializes a <see cref="StackItem"/> from byte array.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="limits">The limits for the deserialization.</param>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The deserialized <see cref="StackItem"/>.</returns>
        public static StackItem Deserialize(ref MemoryReader reader, ExecutionEngineLimits limits, ReferenceCounter referenceCounter = null)
        {
            return Deserialize(ref reader, limits.MaxItemSize, limits.MaxStackSize, referenceCounter);
        }

        /// <summary>
        /// Deserializes a <see cref="StackItem"/> from <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="maxSize">The maximum size of the result.</param>
        /// <param name="maxItems">The max of items to serialize</param>
        /// <param name="referenceCounter">The <see cref="ReferenceCounter"/> used by the <see cref="StackItem"/>.</param>
        /// <returns>The deserialized <see cref="StackItem"/>.</returns>
        public static StackItem Deserialize(ref MemoryReader reader, uint maxSize, uint maxItems, ReferenceCounter referenceCounter)
        {
            Stack<StackItem> deserialized = new();
            int undeserialized = 1;
            while (undeserialized-- > 0)
            {
                StackItemType type = (StackItemType)reader.ReadByte();
                switch (type)
                {
                    case StackItemType.Any:
                        deserialized.Push(StackItem.Null);
                        break;
                    case StackItemType.Boolean:
                        deserialized.Push(reader.ReadBoolean());
                        break;
                    case StackItemType.Integer:
                        deserialized.Push(new BigInteger(reader.ReadVarMemory(Integer.MaxSize).Span));
                        break;
                    case StackItemType.ByteString:
                        deserialized.Push(reader.ReadVarMemory((int)maxSize));
                        break;
                    case StackItemType.Buffer:
                        ReadOnlyMemory<byte> memory = reader.ReadVarMemory((int)maxSize);
                        deserialized.Push(new Buffer(memory.Span));
                        break;
                    case StackItemType.Array:
                    case StackItemType.Struct:
                        {
                            int count = (int)reader.ReadVarInt((ulong)maxItems);
                            deserialized.Push(new ContainerPlaceholder(type, count));
                            undeserialized += count;
                        }
                        break;
                    case StackItemType.Map:
                        {
                            int count = (int)reader.ReadVarInt((ulong)maxItems);
                            deserialized.Push(new ContainerPlaceholder(type, count));
                            undeserialized += count * 2;
                        }
                        break;
                    default:
                        throw new FormatException();
                }
                if (deserialized.Count > maxItems)
                    throw new FormatException();
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
                        case StackItemType.Struct:
                            Struct @struct = new(referenceCounter);
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                @struct.Add(stack_temp.Pop());
                            item = @struct;
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

        /// <summary>
        /// Serializes a <see cref="StackItem"/> to byte array.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to be serialized.</param>
        /// <param name="limits">The <see cref="ExecutionEngineLimits"/> used to ensure the limits.</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize(StackItem item, ExecutionEngineLimits limits)
        {
            return Serialize(item, limits.MaxItemSize, limits.MaxStackSize);
        }

        /// <summary>
        /// Serializes a <see cref="StackItem"/> to byte array.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to be serialized.</param>
        /// <param name="maxSize">The maximum size of the result.</param>
        /// <param name="maxItems">The max of items to serialize</param>
        /// <returns>The serialized byte array.</returns>
        public static byte[] Serialize(StackItem item, long maxSize, long maxItems)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            Serialize(writer, item, maxSize, maxItems);
            writer.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Serializes a <see cref="StackItem"/> into <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="item">The <see cref="StackItem"/> to be serialized.</param>
        /// <param name="maxSize">The maximum size of the result.</param>
        /// <param name="maxItems">The max of items to serialize</param>
        public static void Serialize(BinaryWriter writer, StackItem item, long maxSize, long maxItems)
        {
            HashSet<CompoundType> serialized = new(ReferenceEqualityComparer.Instance);
            Stack<StackItem> unserialized = new();
            unserialized.Push(item);
            while (unserialized.Count > 0)
            {
                item = unserialized.Pop();
                writer.Write((byte)item.Type);
                if (--maxItems < 0)
                    throw new FormatException();
                switch (item)
                {
                    case Null _:
                        break;
                    case Boolean _:
                        writer.Write(item.GetBoolean());
                        break;
                    case Integer _:
                    case ByteString _:
                    case Buffer _:
                        writer.WriteVarBytes(item.GetSpan());
                        break;
                    case Array array:
                        if (!serialized.Add(array))
                            throw new NotSupportedException();
                        writer.WriteVarInt(array.Count);
                        for (int i = array.Count - 1; i >= 0; i--)
                            unserialized.Push(array[i]);
                        break;
                    case Map map:
                        if (!serialized.Add(map))
                            throw new NotSupportedException();
                        writer.WriteVarInt(map.Count);
                        foreach (var pair in map.Reverse())
                        {
                            unserialized.Push(pair.Value);
                            unserialized.Push(pair.Key);
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
                writer.Flush();
                if (writer.BaseStream.Position > maxSize)
                    throw new InvalidOperationException();
            }
        }
    }
}
