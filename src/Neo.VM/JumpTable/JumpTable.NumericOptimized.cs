// Copyright (C) 2015-2025 The Neo Project.
//
// JumpTable.NumericOptimized.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.VM.Types;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Optimized numeric operations with fast paths for integer arithmetic.
    /// This partial class provides performance-optimized versions of numeric operations.
    /// </summary>
    public partial class JumpTable
    {
        /// <summary>
        /// Optimized version of Add operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long arithmetic if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    try
                    {
                        long result = checked(val1 + val2);
                        engine.Push(FastInteger.Create(result));
                        return;
                    }
                    catch (System.OverflowException)
                    {
                        // Fall through to BigInteger path
                    }
                }
            }

            // Fallback to original implementation
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 + x2);
        }

        /// <summary>
        /// Optimized version of Subtract operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SubOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long arithmetic if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    try
                    {
                        long result = checked(val1 - val2);
                        engine.Push(FastInteger.Create(result));
                        return;
                    }
                    catch (System.OverflowException)
                    {
                        // Fall through to BigInteger path
                    }
                }
            }

            // Fallback to original implementation
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 - x2);
        }

        /// <summary>
        /// Optimized version of Multiply operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void MulOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long arithmetic if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    // Check for multiplication by 0, 1, -1 first (very common cases)
                    if (val1 == 0 || val2 == 0)
                    {
                        engine.Push(FastInteger.Zero);
                        return;
                    }
                    if (val1 == 1)
                    {
                        engine.Push(FastInteger.Create(val2));
                        return;
                    }
                    if (val2 == 1)
                    {
                        engine.Push(FastInteger.Create(val1));
                        return;
                    }
                    if (val1 == -1)
                    {
                        engine.Push(FastInteger.Create(-val2));
                        return;
                    }
                    if (val2 == -1)
                    {
                        engine.Push(FastInteger.Create(-val1));
                        return;
                    }

                    try
                    {
                        long result = checked(val1 * val2);
                        engine.Push(FastInteger.Create(result));
                        return;
                    }
                    catch (System.OverflowException)
                    {
                        // Fall through to BigInteger path
                    }
                }
            }

            // Fallback to original implementation
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 * x2);
        }

        /// <summary>
        /// Optimized version of Divide operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DivOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long arithmetic if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    if (val2 == 0)
                        throw new System.DivideByZeroException();

                    // Check for division by 1, -1 (common cases)
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

                    long result = val1 / val2;
                    engine.Push(FastInteger.Create(result));
                    return;
                }
            }

            // Fallback to original implementation
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 / x2);
        }

        /// <summary>
        /// Optimized version of Modulo operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ModOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long arithmetic if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    if (val2 == 0)
                        throw new System.DivideByZeroException();

                    long result = val1 % val2;
                    engine.Push(FastInteger.Create(result));
                    return;
                }
            }

            // Fallback to original implementation
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 % x2);
        }

        /// <summary>
        /// Optimized version of Increment operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IncOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();

            // Fast path: item is integer that can fit in FastInteger
            if (item is Integer intItem)
            {
                var big = intItem.GetInteger();

                // Try fast path with long arithmetic if value fits
                if (TryGetLong(big, out long val))
                {
                    if (val != long.MaxValue)
                    {
                        engine.Push(FastInteger.Create(val + 1));
                        return;
                    }
                }
            }

            // Fallback to original implementation
            var x = item.GetInteger();
            engine.Push(x + 1);
        }

        /// <summary>
        /// Optimized version of Decrement operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DecOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();

            // Fast path: item is integer that can fit in FastInteger
            if (item is Integer intItem)
            {
                var big = intItem.GetInteger();

                // Try fast path with long arithmetic if value fits
                if (TryGetLong(big, out long val))
                {
                    if (val != long.MinValue)
                    {
                        engine.Push(FastInteger.Create(val - 1));
                        return;
                    }
                }
            }

            // Fallback to original implementation
            var x = item.GetInteger();
            engine.Push(x - 1);
        }

        /// <summary>
        /// Optimized version of Negate operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NegateOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item = engine.Pop();

            // Fast path: item is integer that can fit in FastInteger
            if (item is Integer intItem)
            {
                var big = intItem.GetInteger();

                // Try fast path with long arithmetic if value fits
                if (TryGetLong(big, out long val))
                {
                    if (val != long.MinValue)
                    {
                        engine.Push(FastInteger.Create(-val));
                        return;
                    }
                }
            }

            // Fallback to original implementation
            var x = item.GetInteger();
            engine.Push(-x);
        }

        /// <summary>
        /// Optimized version of comparison operations with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NumEqualOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long comparison if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    engine.Push(val1 == val2);
                    return;
                }
            }

            // Fallback to original implementation
            var x2 = item2.GetInteger();
            var x1 = item1.GetInteger();
            engine.Push(x1 == x2);
        }

        /// <summary>
        /// Optimized version of less than operation with fast path for small integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void LtOptimized(ExecutionEngine engine, Instruction instruction)
        {
            var item2 = engine.Pop();
            var item1 = engine.Pop();

            if (item1.IsNull || item2.IsNull)
            {
                engine.Push(false);
                return;
            }

            // Fast path: both items are integers that can fit in FastInteger
            if (item1 is Integer int1 && item2 is Integer int2)
            {
                var big1 = int1.GetInteger();
                var big2 = int2.GetInteger();

                // Try fast path with long comparison if values fit
                if (TryGetLong(big1, out long val1) && TryGetLong(big2, out long val2))
                {
                    engine.Push(val1 < val2);
                    return;
                }
            }

            // Fallback to original implementation
            engine.Push(item1.GetInteger() < item2.GetInteger());
        }

        /// <summary>
        /// Helper method to try converting BigInteger to long if it fits in range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetLong(BigInteger value, out long result)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                result = (long)value;
                return true;
            }
            result = 0;
            return false;
        }
    }
}
