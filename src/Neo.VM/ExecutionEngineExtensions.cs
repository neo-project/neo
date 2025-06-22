// Copyright (C) 2015-2025 The Neo Project.
//
// ExecutionEngineExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Extension methods for ExecutionEngine to support optimized integer operations.
    /// </summary>
    public static class ExecutionEngineExtensions
    {
        /// <summary>
        /// Push an optimized integer onto the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushInteger(this ExecutionEngine engine, long value)
        {
            engine.Push(FastInteger.Create(value));
        }
        
        /// <summary>
        /// Pop an integer and try to get it as long for fast operations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPopLong(this ExecutionEngine engine, out long value)
        {
            var item = engine.Pop();
            if (item is FastInteger fi)
            {
                return fi.TryGetLong(out value);
            }
            else if (item is Integer i)
            {
                var big = i.GetInteger();
                if (big >= long.MinValue && big <= long.MaxValue)
                {
                    value = (long)big;
                    return true;
                }
            }
            
            value = 0;
            return false;
        }
        
        /// <summary>
        /// Pop an integer and get it as int32 for array indexing operations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopInt32(this ExecutionEngine engine)
        {
            var item = engine.Pop();
            if (item is FastInteger fi)
            {
                return fi.GetInt32();
            }
            else if (item is Integer i)
            {
                var big = i.GetInteger();
                if (big >= int.MinValue && big <= int.MaxValue)
                {
                    return (int)big;
                }
                throw new System.OverflowException("Value too large for int32");
            }
            
            var bigInt = item.GetInteger();
            if (bigInt >= int.MinValue && bigInt <= int.MaxValue)
            {
                return (int)bigInt;
            }
            throw new System.OverflowException("Value too large for int32");
        }
        
        /// <summary>
        /// Peek at an integer and try to get it as long for fast operations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPeekLong(this ExecutionEngine engine, int index, out long value)
        {
            var item = engine.Peek(index);
            if (item is FastInteger fi)
            {
                return fi.TryGetLong(out value);
            }
            else if (item is Integer i)
            {
                var big = i.GetInteger();
                if (big >= long.MinValue && big <= long.MaxValue)
                {
                    value = (long)big;
                    return true;
                }
            }
            
            value = 0;
            return false;
        }
    }
}