// Copyright (C) 2015-2025 The Neo Project.
//
// StackItemCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Numerics;

namespace Neo.VM
{
    /// <summary>
    /// Provides caching for commonly used StackItem instances to reduce allocations.
    /// </summary>
    public static class StackItemCache
    {
        private const int MinCachedInteger = -8;
        private const int MaxCachedInteger = 16;
        private static readonly Integer[] CachedIntegers;
        
        /// <summary>
        /// Cached Boolean.True instance.
        /// </summary>
        public static readonly Boolean True = new(true);
        
        /// <summary>
        /// Cached Boolean.False instance.
        /// </summary>
        public static readonly Boolean False = new(false);

        static StackItemCache()
        {
            int count = MaxCachedInteger - MinCachedInteger + 1;
            CachedIntegers = new Integer[count];
            for (int i = 0; i < count; i++)
            {
                CachedIntegers[i] = new Integer(MinCachedInteger + i);
            }
        }

        /// <summary>
        /// Gets a cached Integer instance for small values, or creates a new one for larger values.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A cached or new Integer instance.</returns>
        public static Integer GetInteger(BigInteger value)
        {
            if (value >= MinCachedInteger && value <= MaxCachedInteger)
            {
                return CachedIntegers[(int)value - MinCachedInteger];
            }
            return new Integer(value);
        }

        /// <summary>
        /// Gets a cached Boolean instance.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A cached Boolean instance.</returns>
        public static Boolean GetBoolean(bool value)
        {
            return value ? True : False;
        }
    }
}