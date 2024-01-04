// Copyright (C) 2015-2024 The Neo Project.
//
// ArrayExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Extensions
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        public static string ToHexString(this byte[] value)
        {
            var buf = string.Empty;
            foreach (var b in value)
                buf += $"{b:x02}";
            return buf;
        }

        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <param name="reverse">Indicates whether it should be converted in the reversed byte order.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        public static string ToHexString(this byte[] value, bool reverse = false)
        {
            var buf = string.Empty;
            for (var i = 0; i < value.Length; i++)
                buf += $"{(value[reverse ? value.Length - i - 1 : i]):x02}";
            return buf;
        }
    }
}
