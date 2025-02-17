// Copyright (C) 2015-2025 The Neo Project.
//
// ByteConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using System;
using System.Text;

namespace Neo.Build.Core.Extensions
{
    public static class ByteConverter
    {
        public static string ToHexString(byte[] value, bool upperCase = false)
        {
#if NET9_0_OR_GREATER
            return value
                .TryCatchThrow<FormatException, NeoBuildInvalidHexFormatException>(
                    () => upperCase ?
                        Convert.ToHexString(value) :
                        Convert.ToHexStringLower(value)
                );
#else
            if (value.Length == 0)
                return string.Empty;
            if (value.Length > int.MaxValue >> 1)
                throw new NeoBuildInvalidHexFormatException();
            var sb = new StringBuilder(value.Length * 2);
            foreach (var b in value)
            {
                if (upperCase)
                    sb.AppendFormat("{0:X02}", b);
                else
                    sb.AppendFormat("{0:x02}", b);
            }
            return sb.ToString();
#endif
        }
    }
}
