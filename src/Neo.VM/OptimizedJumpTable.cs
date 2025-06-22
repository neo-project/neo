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
using System.Numerics;
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

        // Optimized Push operations

        /// <summary>
        /// Optimized PUSHINT8 operation using FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushInt8(ExecutionEngine engine, Instruction instruction)
        {
            var value = new BigInteger(instruction.Operand.Span);
            engine.PushInteger(value);
        }

        /// <summary>
        /// Optimized PUSHINT16 operation using FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushInt16(ExecutionEngine engine, Instruction instruction)
        {
            var value = new BigInteger(instruction.Operand.Span);
            engine.PushInteger(value);
        }

        /// <summary>
        /// Optimized PUSHINT32 operation using FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushInt32(ExecutionEngine engine, Instruction instruction)
        {
            var value = new BigInteger(instruction.Operand.Span);
            engine.PushInteger(value);
        }

        /// <summary>
        /// Optimized PUSHINT64 operation using FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushInt64(ExecutionEngine engine, Instruction instruction)
        {
            var value = new BigInteger(instruction.Operand.Span);
            engine.PushInteger(value);
        }

        /// <summary>
        /// Optimized PUSHINT128 operation using FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushInt128(ExecutionEngine engine, Instruction instruction)
        {
            var value = new BigInteger(instruction.Operand.Span);
            engine.PushInteger(value);
        }

        /// <summary>
        /// Optimized PUSHINT256 operation using FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushInt256(ExecutionEngine engine, Instruction instruction)
        {
            var value = new BigInteger(instruction.Operand.Span);
            engine.PushInteger(value);
        }

        // Optimized constant push operations using cached FastInteger instances

        /// <summary>
        /// Optimized PUSHM1 operation using cached FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void PushM1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.MinusOne);
        }

        /// <summary>
        /// Optimized PUSH0 operation using cached FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Zero);
        }

        /// <summary>
        /// Optimized PUSH1 operation using cached FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.One);
        }

        /// <summary>
        /// Optimized PUSH2 through PUSH16 operations using cached FastInteger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push3(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(3));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push5(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(5));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push6(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(6));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push7(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(7));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push9(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(9));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push10(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(10));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push11(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(11));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push12(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(12));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push13(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(13));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push14(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(14));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push15(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(15));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Push16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(FastInteger.Create(16));
        }
    }
}
