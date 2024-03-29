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

        /// <summary>
        /// Packs a struct from the evaluation stack.
        /// <see cref="OpCode.PACKSTRUCT"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Packs an array from the evaluation stack.
        /// <see cref="OpCode.PACK"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Unpacks a compound type from the evaluation stack.
        /// <see cref="OpCode.UNPACK"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NewArray(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {n}");

            engine.Push(new VMArray(engine.ReferenceCounter, Enumerable.Repeat(StackItem.Null, n)));
        }

        /// <summary>
        /// Creates a new array with a specified number of elements and a specified type on the evaluation stack.
        /// <see cref="OpCode.NEWARRAY_T"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Creates a new empty struct with zero elements on the evaluation stack.
        /// <see cref="OpCode.NEWSTRUCT0"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Creates a new empty map on the evaluation stack.
        /// <see cref="OpCode.NEWMAP"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Checks whether the top item on the evaluation stack has the specified key.
        /// <see cref="OpCode.HASKEY"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
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
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < buffer.Size);
                        break;
                    }
                // For byte strings, check if the index is within bounds and push the result onto the stack.
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

        /// <summary>
        /// Retrieves the keys of a map and pushes them onto the evaluation stack as an array.
        /// <see cref="OpCode.KEYS"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Appends an item to the end of the specified array.
        /// <see cref="OpCode.APPEND"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Append(ExecutionEngine engine, Instruction instruction)
        {
            var newItem = engine.Pop();
            var array = engine.Pop<VMArray>();
            if (newItem is Struct s) newItem = s.Clone(engine.Limits);
            array.Add(newItem);
        }

        /// <summary>
        /// Sets the item at the specified index in the array, map, or buffer to the specified value.
        /// <see cref="OpCode.SETITEM"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Reverses the order of items in the specified array or buffer.
        /// <see cref="OpCode.REVERSEITEMS"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Removes the item at the specified index from the array or map.
        /// <see cref="OpCode.REMOVE"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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

        /// <summary>
        /// Clears all items from the compound type.
        /// <see cref="OpCode.CLEARITEMS"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ClearItems(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.Pop<CompoundType>();
            x.Clear();
        }

        /// <summary>
        /// Removes and returns the item at the top of the specified array.
        /// <see cref="OpCode.POPITEM"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
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
