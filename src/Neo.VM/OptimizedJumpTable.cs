// Copyright (C) 2015-2025 The Neo Project.
//
// OptimizedJumpTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Optimized jump table that uses fast paths for integer operations.
    /// This class extends the base JumpTable with performance optimizations.
    /// </summary>
    public class OptimizedJumpTable : JumpTable
    {
        /// <summary>
        /// Optimized Add operation that uses fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Add(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            // Try optimized arithmetic first
            if (IntegerFactory.TryAdd(item1, item2, out var result))
            {
                engine.Push(result);
                return;
            }
            
            // Fall back to BigInteger arithmetic
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(IntegerFactory.Create(x1 + x2));
        }
        
        /// <summary>
        /// Optimized Subtract operation that uses fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Sub(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            // Try optimized arithmetic first
            if (IntegerFactory.TrySubtract(item1, item2, out var result))
            {
                engine.Push(result);
                return;
            }
            
            // Fall back to BigInteger arithmetic
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(IntegerFactory.Create(x1 - x2));
        }
        
        /// <summary>
        /// Optimized Multiply operation that uses fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Mul(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            // Try optimized arithmetic first
            if (IntegerFactory.TryMultiply(item1, item2, out var result))
            {
                engine.Push(result);
                return;
            }
            
            // Fall back to BigInteger arithmetic
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(IntegerFactory.Create(x1 * x2));
        }
        
        /// <summary>
        /// Optimized Divide operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Div(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                if (val2 == 0)
                    throw new DivideByZeroException();
                
                // Handle common cases
                if (val2 == 1)
                {
                    engine.Push(FastInteger.Create(val1));
                    return;
                }
                if (val2 == -1)
                {
                    engine.Push(FastInteger.Create(-val1));
                    return;
                }
                
                engine.Push(FastInteger.Create(val1 / val2));
                return;
            }
            
            // Fall back to BigInteger arithmetic
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(IntegerFactory.Create(x1 / x2));
        }
        
        /// <summary>
        /// Optimized Modulo operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Mod(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                if (val2 == 0)
                    throw new DivideByZeroException();
                
                engine.Push(FastInteger.Create(val1 % val2));
                return;
            }
            
            // Fall back to BigInteger arithmetic
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(IntegerFactory.Create(x1 % x2));
        }
        
        /// <summary>
        /// Optimized Increment operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Inc(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item, out long val))
            {
                if (val != long.MaxValue)
                {
                    engine.Push(FastInteger.Create(val + 1));
                    return;
                }
            }
            
            // Fall back to BigInteger arithmetic
            var x = item.GetInteger();
            engine.Push(IntegerFactory.Create(x + 1));
        }
        
        /// <summary>
        /// Optimized Decrement operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Dec(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item, out long val))
            {
                if (val != long.MinValue)
                {
                    engine.Push(FastInteger.Create(val - 1));
                    return;
                }
            }
            
            // Fall back to BigInteger arithmetic
            var x = item.GetInteger();
            engine.Push(IntegerFactory.Create(x - 1));
        }
        
        /// <summary>
        /// Optimized Negate operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Negate(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item, out long val))
            {
                if (val != long.MinValue)
                {
                    engine.Push(FastInteger.Create(-val));
                    return;
                }
            }
            
            // Fall back to BigInteger arithmetic
            var x = item.GetInteger();
            engine.Push(IntegerFactory.Create(-x));
        }
        
        /// <summary>
        /// Optimized NumEqual operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void NumEqual(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                engine.Push(val1 == val2);
                return;
            }
            
            // Fall back to BigInteger comparison
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 == x2);
        }
        
        /// <summary>
        /// Optimized Lt operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Lt(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            if (item1.IsNull || item2.IsNull)
            {
                engine.Push(false);
                return;
            }
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                engine.Push(val1 < val2);
                return;
            }
            
            // Fall back to BigInteger comparison
            engine.Push(item1.GetInteger() < item2.GetInteger());
        }
        
        /// <summary>
        /// Optimized Le operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Le(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            if (item1.IsNull || item2.IsNull)
            {
                engine.Push(false);
                return;
            }
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                engine.Push(val1 <= val2);
                return;
            }
            
            // Fall back to BigInteger comparison
            engine.Push(item1.GetInteger() <= item2.GetInteger());
        }
        
        /// <summary>
        /// Optimized Gt operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Gt(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            if (item1.IsNull || item2.IsNull)
            {
                engine.Push(false);
                return;
            }
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                engine.Push(val1 > val2);
                return;
            }
            
            // Fall back to BigInteger comparison
            engine.Push(item1.GetInteger() > item2.GetInteger());
        }
        
        /// <summary>
        /// Optimized Ge operation with fast paths for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Ge(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();
            
            if (item1.IsNull || item2.IsNull)
            {
                engine.Push(false);
                return;
            }
            
            // Fast path for long values
            if (IntegerFactory.TryGetLongValue(item1, out long val1) && 
                IntegerFactory.TryGetLongValue(item2, out long val2))
            {
                engine.Push(val1 >= val2);
                return;
            }
            
            // Fall back to BigInteger comparison
            engine.Push(item1.GetInteger() >= item2.GetInteger());
        }
        
        /// <summary>
        /// Optimized XDrop operation with fast int32 conversion for indexing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void XDrop(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();
            
            // Fast path for int32 values
            if (IntegerFactory.TryGetInt32Value(item, out int n))
            {
                if (n < 0)
                    throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
                engine.CurrentContext!.EvaluationStack.Remove<StackItem>(n);
                return;
            }
            
            // Fall back to BigInteger conversion
            var bigN = (int)item.GetInteger();
            if (bigN < 0)
                throw new InvalidOperationException($"The negative value {bigN} is invalid for OpCode.{instruction.OpCode}.");
            engine.CurrentContext!.EvaluationStack.Remove<StackItem>(bigN);
        }
        
        /// <summary>
        /// Optimized Pick operation with fast int32 conversion for indexing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Pick(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();
            
            // Fast path for int32 values
            if (IntegerFactory.TryGetInt32Value(item, out int n))
            {
                if (n < 0)
                    throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
                engine.Push(engine.Peek(n));
                return;
            }
            
            // Fall back to BigInteger conversion
            var bigN = (int)item.GetInteger();
            if (bigN < 0)
                throw new InvalidOperationException($"The negative value {bigN} is invalid for OpCode.{instruction.OpCode}.");
            engine.Push(engine.Peek(bigN));
        }
    }
}