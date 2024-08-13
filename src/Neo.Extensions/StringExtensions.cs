// Copyright (C) 2015-2024 The Neo Project.
//
// StringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Globalization;

namespace Neo.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a hex <see cref="string"/> to byte array.
        /// </summary>
        /// <param name="value">The hex <see cref="string"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] HexToBytes(this string value)
        {
            if (value == null || value.Length == 0)
                return [];
            if (value.Length % 2 == 1)
                throw new FormatException();
            var result = new byte[value.Length / 2];
            for (var i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        /// <summary>
        /// Gets the size of the specified <see cref="string"/> encoded in variable-length encoding.
        /// </summary>
        /// <param name="value">The specified <see cref="string"/>.</param>
        /// <returns>The size of the <see cref="string"/>.</returns>
        public static int GetVarSize(this string value)
        {
            var size = Utility.StrictUTF8.GetByteCount(value);
            return UnsafeData.GetVarSize(size) + size;
        }
    }
}
