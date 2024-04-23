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

namespace Neo.Hosting.App.Extensions
{
    internal static class StringExtensions
    {
        public static string? PadCenter(this string? str, int totalWidth, char paddingChar)
        {
            if (str is null) return str;

            if (str.Length >= totalWidth)
                return str;

            var padLeft = (totalWidth - str.Length) / 2 + str.Length;
            return str.PadLeft(padLeft, paddingChar).PadRight(totalWidth, paddingChar);
        }
    }
}
