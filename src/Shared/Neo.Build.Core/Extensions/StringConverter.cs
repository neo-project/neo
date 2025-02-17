// Copyright (C) 2015-2025 The Neo Project.
//
// StringConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using System;
using System.Globalization;

namespace Neo.Build.Core.Extensions
{
    public static class StringConverter
    {
        public static byte[] FromHexString(this ReadOnlySpan<char> value)
        {
#if NET5_0_OR_GREATER
            return Convert.FromHexString(value);
#else
            if (value.IsEmpty)
                return [];
            if ((uint)value.Length % 2 != 0)
                throw new NeoBuildInvalidHexFormatException();
            var result = new byte[value.Length >> 1];
            for (var i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Slice(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
#endif
        }

        public static byte[] FromHexString(this string value) =>
            FromHexString(value.AsSpan());
    }
}
