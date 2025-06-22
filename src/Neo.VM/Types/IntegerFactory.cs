// Copyright (C) 2015-2025 The Neo Project.
//
// IntegerFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Factory class for creating optimized integer instances.
    /// Automatically chooses between FastInteger and Integer based on value characteristics.
    /// </summary>
    public static class IntegerFactory
    {
        /// <summary>
        /// Create an optimized integer from a BigInteger value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackItem Create(BigInteger value)
        {
            // Use FastInteger for values that can fit in long range
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                return FastInteger.Create((long)value);
            }

            // Fall back to original Integer for very large values
            return new Integer(value);
        }

        /// <summary>
        /// Create an optimized integer from a byte array (commonly used in VM operations).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackItem Create(ReadOnlySpan<byte> data)
        {
            var value = new BigInteger(data);
            return Create(value);
        }

        /// <summary>
        /// Create an optimized integer from a long value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackItem Create(long value)
        {
            return FastInteger.Create(value);
        }

        /// <summary>
        /// Create an optimized integer from an int value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackItem Create(int value)
        {
            return FastInteger.Create((long)value);
        }

        /// <summary>
        /// Create an optimized integer from a uint value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackItem Create(uint value)
        {
            return FastInteger.Create((long)value);
        }

        /// <summary>
        /// Create an optimized integer from a ulong value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackItem Create(ulong value)
        {
            if (value <= long.MaxValue)
            {
                return FastInteger.Create((long)value);
            }

            return new Integer(new BigInteger(value));
        }

        /// <summary>
        /// Try to perform optimized arithmetic between two integer stack items.
        /// Returns true if optimization was applied, false if fallback is needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd(StackItem left, StackItem right, out StackItem result)
        {
            if (TryGetLongValue(left, out long leftVal) && TryGetLongValue(right, out long rightVal))
            {
                try
                {
                    long sum = checked(leftVal + rightVal);
                    result = FastInteger.Create(sum);
                    return true;
                }
                catch (System.OverflowException)
                {
                    // Fall back to BigInteger arithmetic
                }
            }

            result = null!;
            return false;
        }

        /// <summary>
        /// Try to perform optimized subtraction between two integer stack items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySubtract(StackItem left, StackItem right, out StackItem result)
        {
            if (TryGetLongValue(left, out long leftVal) && TryGetLongValue(right, out long rightVal))
            {
                try
                {
                    long diff = checked(leftVal - rightVal);
                    result = FastInteger.Create(diff);
                    return true;
                }
                catch (System.OverflowException)
                {
                    // Fall back to BigInteger arithmetic
                }
            }

            result = null!;
            return false;
        }

        /// <summary>
        /// Try to perform optimized multiplication between two integer stack items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryMultiply(StackItem left, StackItem right, out StackItem result)
        {
            if (TryGetLongValue(left, out long leftVal) && TryGetLongValue(right, out long rightVal))
            {
                // Handle common special cases first
                if (leftVal == 0 || rightVal == 0)
                {
                    result = FastInteger.Zero;
                    return true;
                }
                if (leftVal == 1)
                {
                    result = FastInteger.Create(rightVal);
                    return true;
                }
                if (rightVal == 1)
                {
                    result = FastInteger.Create(leftVal);
                    return true;
                }
                if (leftVal == -1)
                {
                    result = FastInteger.Create(-rightVal);
                    return true;
                }
                if (rightVal == -1)
                {
                    result = FastInteger.Create(-leftVal);
                    return true;
                }

                try
                {
                    long product = checked(leftVal * rightVal);
                    result = FastInteger.Create(product);
                    return true;
                }
                catch (System.OverflowException)
                {
                    // Fall back to BigInteger arithmetic
                }
            }

            result = null!;
            return false;
        }

        /// <summary>
        /// Try to get the long value from a StackItem if it's an integer that fits in long range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetLongValue(StackItem item, out long value)
        {
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
        /// Try to get the int32 value from a StackItem for array indexing operations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetInt32Value(StackItem item, out int value)
        {
            if (item is FastInteger fi)
            {
                try
                {
                    value = fi.GetInt32();
                    return true;
                }
                catch (System.OverflowException)
                {
                    value = 0;
                    return false;
                }
            }
            else if (item is Integer i)
            {
                var big = i.GetInteger();
                if (big >= int.MinValue && big <= int.MaxValue)
                {
                    value = (int)big;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Perform optimized comparison between two integer stack items.
        /// Returns true if optimization was applied, false if fallback is needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCompare(StackItem left, StackItem right, out int result)
        {
            if (TryGetLongValue(left, out long leftVal) && TryGetLongValue(right, out long rightVal))
            {
                result = leftVal.CompareTo(rightVal);
                return true;
            }

            result = 0;
            return false;
        }
    }
}
