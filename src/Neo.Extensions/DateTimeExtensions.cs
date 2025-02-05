// Copyright (C) 2015-2025 The Neo Project.
//
// DateTimeExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DateTime"/> to timestamp.
        /// </summary>
        /// <param name="time">The <see cref="DateTime"/> to convert.</param>
        /// <returns>The converted timestamp.</returns>
        public static uint ToTimestamp(this DateTime time)
        {
            return (uint)new DateTimeOffset(time.ToUniversalTime()).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to timestamp in milliseconds.
        /// </summary>
        /// <param name="time">The <see cref="DateTime"/> to convert.</param>
        /// <returns>The converted timestamp.</returns>
        public static ulong ToTimestampMS(this DateTime time)
        {
            return (ulong)new DateTimeOffset(time.ToUniversalTime()).ToUnixTimeMilliseconds();
        }
    }
}
