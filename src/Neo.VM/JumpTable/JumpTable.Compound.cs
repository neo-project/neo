// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Compound.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    public partial class JumpTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PackMap(ExecutionEngine engine, Instruction instruction)
        {
            var size = (int)engine.Pop().GetInteger();
            if (size < 0 || size * 2 > engine.CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The value {size} is out of range.");
            Map map = new(engine.ReferenceCounter);
            for (var i = 0; i < size; i++)
            {
                var key = engine.Pop<PrimitiveType>();
                var value = engine.Pop();
                map[key] = value;
            }
            engine.Push(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PackStruct(ExecutionEngine engine, Instruction instruction)
        {
            var size = (int)engine.Pop().GetInteger();
            if (size < 0 || size > engine.CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The value {size} is out of range.");
            Struct @struct = new(engine.ReferenceCounter);
            for (var i = 0; i < size; i++)
            {
                var item = engine.Pop();
                @struct.Add(item);
            }
            engine.Push(@struct);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Pack(ExecutionEngine engine, Instruction instruction)
        {
            var size = (int)engine.Pop().GetInteger();
            if (size < 0 || size > engine.CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The value {size} is out of range.");
            VMArray array = new(engine.ReferenceCounter);
            for (var i = 0; i < size; i++)
            {
                var item = engine.Pop();
                array.Add(item);
            }
            engine.Push(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Unpack(ExecutionEngine engine, Instruction instruction)
        {
            var compound = engine.Pop<CompoundType>();
            switch (compound)
            {
                case Map map:
                    foreach (var (key, value) in map.Reverse())
                    {
                        engine.Push(value);
                        engine.Push(key);
                    }
                    break;
                case VMArray array:
                    for (var i = array.Count - 1; i >= 0; i--)
                    {
                        engine.Push(array[i]);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {compound.Type}");
            }
            engine.Push(compound.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new VMArray(engine.ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {n}");

            engine.Push(new VMArray(engine.ReferenceCounter, Enumerable.Repeat(StackItem.Null, n)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray_T(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {n}");

            var type = (StackItemType)instruction.TokenU8;
            if (!Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {instruction.TokenU8}");

            var item = instruction.TokenU8 switch
            {
                (byte)StackItemType.Boolean => StackItem.False,
                (byte)StackItemType.Integer => Integer.Zero,
                (byte)StackItemType.ByteString => ByteString.Empty,
                _ => StackItem.Null
            };

            engine.Push(new VMArray(engine.ReferenceCounter, Enumerable.Repeat(item, n)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewStruct0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new Struct(engine.ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewStruct(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {n}");
            Struct result = new(engine.ReferenceCounter);
            for (var i = 0; i < n; i++)
                result.Add(StackItem.Null);
            engine.Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewMap(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new Map(engine.ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Size(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            switch (x)
            {
                case CompoundType compound:
                    engine.Push(compound.Count);
                    break;
                case PrimitiveType primitive:
                    engine.Push(primitive.Size);
                    break;
                case Types.Buffer buffer:
                    engine.Push(buffer.Size);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void HasKey(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < array.Count);
                        break;
                    }
                case Map map:
                    {
                        engine.Push(map.ContainsKey(key));
                        break;
                    }
                case Types.Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < buffer.Size);
                        break;
                    }
                case ByteString array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < array.Size);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Keys(ExecutionEngine engine, Instruction instruction)
        {
            var map = engine.Pop<Map>();
            engine.Push(new VMArray(engine.ReferenceCounter, map.Keys));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Values(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            var values = x switch
            {
                VMArray array => array,
                Map map => map.Values,
                _ => throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}"),
            };
            VMArray newArray = new(engine.ReferenceCounter);
            foreach (var item in values)
                if (item is Struct s)
                    newArray.Add(s.Clone(engine.Limits));
                else
                    newArray.Add(item);
            engine.Push(newArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PickItem(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The value {index} is out of range.");
                        engine.Push(array[index]);
                        break;
                    }
                case Map map:
                    {
                        if (!map.TryGetValue(key, out var value))
                            throw new CatchableException($"Key not found in {nameof(Map)}");
                        engine.Push(value);
                        break;
                    }
                case PrimitiveType primitive:
                    {
                        var byteArray = primitive.GetSpan();
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= byteArray.Length)
                            throw new CatchableException($"The value {index} is out of range.");
                        engine.Push((BigInteger)byteArray[index]);
                        break;
                    }
                case Types.Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The value {index} is out of range.");
                        engine.Push((BigInteger)buffer.InnerBuffer.Span[index]);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Append(ExecutionEngine engine, Instruction instruction)
        {
            var newItem = engine.Pop();
            var array = engine.Pop<VMArray>();
            if (newItem is Struct s) newItem = s.Clone(engine.Limits);
            array.Add(newItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetItem(ExecutionEngine engine, Instruction instruction)
        {
            var value = engine.Pop();
            if (value is Struct s) value = s.Clone(engine.Limits);
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The value {index} is out of range.");
                        array[index] = value;
                        break;
                    }
                case Map map:
                    {
                        map[key] = value;
                        break;
                    }
                case Types.Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The value {index} is out of range.");
                        if (value is not PrimitiveType p)
                            throw new InvalidOperationException($"Value must be a primitive type in {instruction.OpCode}");
                        var b = (int)p.GetInteger();
                        if (b < sbyte.MinValue || b > byte.MaxValue)
                            throw new InvalidOperationException($"Overflow in {instruction.OpCode}, {b} is not a byte type.");
                        buffer.InnerBuffer.Span[index] = (byte)b;
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ReverseItems(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    array.Reverse();
                    break;
                case Types.Buffer buffer:
                    buffer.InnerBuffer.Span.Reverse();
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Remove(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    var index = (int)key.GetInteger();
                    if (index < 0 || index >= array.Count)
                        throw new InvalidOperationException($"The value {index} is out of range.");
                    array.RemoveAt(index);
                    break;
                case Map map:
                    map.Remove(key);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ClearItems(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop<CompoundType>();
            x.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PopItem(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop<VMArray>();
            var index = x.Count - 1;
            engine.Push(x[index]);
            x.RemoveAt(index);
        }
    }
}
