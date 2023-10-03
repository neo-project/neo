// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using System;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract library that provides useful regex functions.
    /// By default, the maximum input length is 512 bytes.
    /// We need to break regex functions into smaller pieces to calculate the gas cost.
    /// </summary>
    public sealed class Regex : NativeContract
    {
        private const int MaxInputLength = 512; // make it smaller

        internal Regex() { }

        /// <summary>
        /// Determines whether the beginning of this string instance matches the specified string.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <param name="value">The string to match.</param>
        /// <returns>Whether [str] starts with [value]</returns>
        [ContractMethod(CpuFee = 1 << 8)]
        private static bool StartsWith([MaxLength(MaxInputLength)] string str, [MaxLength(MaxInputLength)] string value)
        {
            return str.StartsWith(value);
        }

        /// <summary>
        /// Determines whether the end of this string instance matches the specified string.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <param name="value">The string to match.</param>
        /// <returns>Whether [str] ends with [value]</returns>
        [ContractMethod(CpuFee = 1 << 8)]
        private static bool EndsWith([MaxLength(MaxInputLength)] string str, [MaxLength(MaxInputLength)] string value)
        {
            return str.EndsWith(value);
        }

        /// <summary>
        /// Determines whether the string instance contains the specified string.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <param name="value">The string to match.</param>
        /// <returns>Whether [str] contains [value]</returns>
        [ContractMethod(CpuFee = 1 << 8)]
        private static bool Contains([MaxLength(MaxInputLength)] string str, [MaxLength(MaxInputLength)] string value)
        {
            return str.Contains(value);
        }

        /// <summary>
        /// Get the index of a substring of the given string.
        /// </summary>
        /// <param name="str">The string to process</param>
        /// <param name="value">The substring to match</param>
        /// <returns>The index of the substring</returns>
        [ContractMethod(CpuFee = 1 << 8)]
        private static int IndexOf([MaxLength(MaxInputLength)] string str, [MaxLength(MaxInputLength)] string value)
        {
            return str.IndexOf(value, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Get a substring of the given string
        /// </summary>
        /// <param name="str"> String to process</param>
        /// <param name="startIndex"> Start index of the substring</param>
        /// <param name="length">length of the substring</param>
        /// <returns>The substring</returns>
        [ContractMethod(CpuFee = 1 << 8)]
        private static string Substring([MaxLength(MaxInputLength)] string str, int startIndex, int length)
        {
            return str.Substring(startIndex, length);
        }
    }
}
