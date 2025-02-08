// Copyright (C) 2015-2025 The Neo Project.
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
using Array = System.Array;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    /// <summary>
    /// Partial class for manipulating compound types like maps, arrays, and structs within a jump table.
    /// </summary>
    public partial class JumpTable
    {
        /// <summary>
        /// Packs a map from the evaluation stack.
        /// <see cref="OpCode.PACKMAP"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 2n+1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PackMap(ExecutionEngine engine, Instruction instruction)
        {
            var size = (int)engine.Pop().GetInteger();
            if (size < 0 || size * 2 > engine.CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The map size is out of valid range, 2*{size}/[0, {engine.CurrentContext!.EvaluationStack.Count}].");
            Map map = new(engine.ReferenceCounter);
            for (var i = 0; i < size; i++)
            {
                var key = engine.Pop<PrimitiveType>();
                var value = engine.Pop();
                map[key] = value;
            }
            engine.Push(map);
        }

        /// <summary>
        /// Packs a struct from the evaluation stack.
        /// <see cref="OpCode.PACKSTRUCT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop n+1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PackStruct(ExecutionEngine engine, Instruction instruction)
        {
            var size = (int)engine.Pop().GetInteger();
            if (size < 0 || size > engine.CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The struct size is out of valid range, {size}/[0, {engine.CurrentContext!.EvaluationStack.Count}].");
            Struct @struct = new(engine.ReferenceCounter);
            for (var i = 0; i < size; i++)
            {
                var item = engine.Pop();
                @struct.Add(item);
            }
            engine.Push(@struct);
        }

        /// <summary>
        /// Packs an array from the evaluation stack.
        /// <see cref="OpCode.PACK"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop n+1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Pack(ExecutionEngine engine, Instruction instruction)
        {
            var size = (int)engine.Pop().GetInteger();
            if (size < 0 || size > engine.CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The array size is out of valid range, {size}/[0, {engine.CurrentContext!.EvaluationStack.Count}].");
            VMArray array = new(engine.ReferenceCounter);
            for (var i = 0; i < size; i++)
            {
                var item = engine.Pop();
                array.Add(item);
            }
            engine.Push(array);
        }

        /// <summary>
        /// Unpacks a compound type from the evaluation stack.
        /// <see cref="OpCode.UNPACK"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 2n+1 or n+1</remarks>
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

        /// <summary>
        /// Creates a new empty array with zero elements on the evaluation stack.
        /// <see cref="OpCode.NEWARRAY0"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>
        /// Pop 0, Push 1
        /// TODO: Change to NewNullArray method or add it?
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new VMArray(engine.ReferenceCounter));
        }

        /// <summary>
        /// Creates a new array with a specified number of elements on the evaluation stack.
        /// <see cref="OpCode.NEWARRAY"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"The array size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");
            var nullArray = new StackItem[n];
            Array.Fill(nullArray, StackItem.Null);
            engine.Push(new VMArray(engine.ReferenceCounter, nullArray));
        }

        /// <summary>
        /// Creates a new array with a specified number of elements and a specified type on the evaluation stack.
        /// <see cref="OpCode.NEWARRAY_T"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray_T(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"The array size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");

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
            var itemArray = new StackItem[n];
            Array.Fill(itemArray, item);
            engine.Push(new VMArray(engine.ReferenceCounter, itemArray));
        }

        /// <summary>
        /// Creates a new empty struct with zero elements on the evaluation stack.
        /// <see cref="OpCode.NEWSTRUCT0"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 0, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewStruct0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new Struct(engine.ReferenceCounter));
        }

        /// <summary>
        /// Creates a new struct with a specified number of elements on the evaluation stack.
        /// <see cref="OpCode.NEWSTRUCT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewStruct(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"The struct size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");

            var nullArray = new StackItem[n];
            Array.Fill(nullArray, StackItem.Null);
            engine.Push(new Struct(engine.ReferenceCounter, nullArray));
        }

        /// <summary>
        /// Creates a new empty map on the evaluation stack.
        /// <see cref="OpCode.NEWMAP"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 0, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewMap(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new Map(engine.ReferenceCounter));
        }

        /// <summary>
        /// Gets the size of the top item on the evaluation stack and pushes it onto the stack.
        /// <see cref="OpCode.SIZE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Size(ExecutionEngine engine, Instruction instruction)
        {
            // TODO: we should be able to optimize by using peek instead of dup and pop
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

        /// <summary>
        /// Checks whether the top item on the evaluation stack has the specified key.
        /// <see cref="OpCode.HASKEY"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 2, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void HasKey(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            // Check the type of the top item and perform the corresponding action.
            switch (x)
            {
                // For arrays, check if the index is within bounds and push the result onto the stack.
                case VMArray array:
                    {
                        // TODO: Overflow and underflow checking needs to be done.
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative index {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < array.Count);
                        break;
                    }
                // For maps, check if the key exists and push the result onto the stack.
                case Map map:
                    {
                        engine.Push(map.ContainsKey(key));
                        break;
                    }
                // For buffers, check if the index is within bounds and push the result onto the stack.
                case Types.Buffer buffer:
                    {
                        // TODO: Overflow and underflow checking needs to be done.
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative index {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < buffer.Size);
                        break;
                    }
                // For byte strings, check if the index is within bounds and push the result onto the stack.
                case ByteString array:
                    {
                        // TODO: Overflow and underflow checking needs to be done.
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative index {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < array.Size);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        /// <summary>
        /// Retrieves the keys of a map and pushes them onto the evaluation stack as an array.
        /// <see cref="OpCode.KEYS"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Keys(ExecutionEngine engine, Instruction instruction)
        {
            var map = engine.Pop<Map>();
            engine.Push(new VMArray(engine.ReferenceCounter, map.Keys));
        }

        /// <summary>
        /// Retrieves the values of a compound type and pushes them onto the evaluation stack as an array.
        /// <see cref="OpCode.VALUES"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
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

        /// <summary>
        /// Retrieves the item from an array, map, buffer, or byte string based on the specified key,
        /// and pushes it onto the evaluation stack.
        /// <see cref="OpCode.PICKITEM"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 2, Push 1</remarks>
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
                            throw new CatchableException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");
                        engine.Push(array[index]);
                        break;
                    }
                case Map map:
                    {
                        if (!map.TryGetValue(key, out var value))
                            throw new CatchableException($"Key {key} not found in {nameof(Map)}.");
                        engine.Push(value);
                        break;
                    }
                case PrimitiveType primitive:
                    {
                        var byteArray = primitive.GetSpan();
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= byteArray.Length)
                            throw new CatchableException($"The index of {nameof(PrimitiveType)} is out of range, {index}/[0, {byteArray.Length}).");
                        engine.Push((BigInteger)byteArray[index]);
                        break;
                    }
                case Types.Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The index of {nameof(Types.Buffer)} is out of range, {index}/[0, {buffer.Size}).");
                        engine.Push((BigInteger)buffer.InnerBuffer.Span[index]);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        /// <summary>
        /// Appends an item to the end of the specified array.
        /// <see cref="OpCode.APPEND"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 2, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Append(ExecutionEngine engine, Instruction instruction)
        {
            var newItem = engine.Pop();
            var array = engine.Pop<VMArray>();
            if (newItem is Struct s) newItem = s.Clone(engine.Limits);
            array.Add(newItem);
            if (engine.ReferenceCounter.Version == RCVersion.V2)
                engine.ReferenceCounter.AddStackReference(newItem);
        }

        /// <summary>
        /// A value v, index n (or key) and an array (or map) are taken from main stack. Attribution array[n]=v (or map[n]=v) is performed.
        /// <see cref="OpCode.SETITEM"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 3, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetItem(ExecutionEngine engine, Instruction instruction)
        {
            var value = engine.Pop();
            if (value is Struct s) value = s.Clone(engine.Limits);
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            var isRC2 = engine.ReferenceCounter.Version == RCVersion.V2;
            switch (x)
            {
                case VMArray array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");
                        if (isRC2)
                            engine.ReferenceCounter.RemoveStackReference(array[index]);
                        array[index] = value;
                        if (isRC2)
                            engine.ReferenceCounter.AddStackReference(value);
                        break;
                    }
                case Map map:
                    {
                        if (isRC2)
                        {
                            if (!map.TryGetValue(key, out var value1))
                            {
                                engine.ReferenceCounter.AddStackReference(key);
                            }
                            else
                            {
                                engine.ReferenceCounter.RemoveStackReference(value1);
                            }
                        }

                        map[key] = value;
                        if (isRC2)
                            engine.ReferenceCounter.AddStackReference(value);
                        break;
                    }
                case Types.Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The index of {nameof(Types.Buffer)} is out of range, {index}/[0, {buffer.Size}).");
                        if (value is not PrimitiveType p)
                            throw new InvalidOperationException($"Only primitive type values can be set in {nameof(Types.Buffer)} in {instruction.OpCode}.");
                        var b = (int)p.GetInteger();
                        if (b < sbyte.MinValue || b > byte.MaxValue)
                            throw new InvalidOperationException($"Overflow in {instruction.OpCode}, {b} is not a byte type.");
                        buffer.InnerBuffer.Span[index] = (byte)b;
                        buffer.InvalidateHashCode();
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        /// <summary>
        /// Reverses the order of items in the specified array or buffer.
        /// <see cref="OpCode.REVERSEITEMS"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 0</remarks>
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
                    buffer.InvalidateHashCode();
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        /// <summary>
        /// Removes the item at the specified index from the array or map.
        /// <see cref="OpCode.REMOVE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 2, Push 0</remarks>
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
                        throw new InvalidOperationException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");
                    var item = array[index];
                    array.RemoveAt(index);
                    if (engine.ReferenceCounter.Version == RCVersion.V2)
                        engine.ReferenceCounter.RemoveStackReference(item);
                    break;
                case Map map:
                    var old = map.RemoveKey(key);
                    if (old != null && engine.ReferenceCounter.Version == RCVersion.V2)
                    {
                        engine.ReferenceCounter.RemoveStackReference(key);
                        engine.ReferenceCounter.RemoveStackReference(old);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        /// <summary>
        /// Clears all items from the compound type.
        /// <see cref="OpCode.CLEARITEMS"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 0</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ClearItems(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop<CompoundType>();
            if (engine.ReferenceCounter.Version == RCVersion.V2)
            {
                foreach (var xSubItem in x.SubItems)
                {
                    engine.ReferenceCounter.RemoveStackReference(xSubItem);
                }
            }

            x.Clear();
        }

        /// <summary>
        /// Removes and returns the item at the top of the specified array.
        /// <see cref="OpCode.POPITEM"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 1, Push 1</remarks>
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
