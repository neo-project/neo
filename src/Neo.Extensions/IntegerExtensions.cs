// Copyright (C) 2015-2025 The Neo Project.
//
// IntegerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    public static class IntegerExtensions
    {
        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetVarSize(this int value) => ((long)value).GetVarSize();

        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetVarSize(this ushort value) => ((long)value).GetVarSize();

        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetVarSize(this uint value) => ((long)value).GetVarSize();

        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        public static byte GetVarSize(this long value)
        {
            if (value < 0xFD)
                return sizeof(byte);
            else if (value <= ushort.MaxValue)
                return sizeof(byte) + sizeof(ushort);
            else if (value <= uint.MaxValue)
                return sizeof(byte) + sizeof(uint);
            else
                return sizeof(byte) + sizeof(ulong);
        }
    }
}
