// Copyright (C) 2015-2025 The Neo Project.
//
// StringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine.Rendering;

namespace Neo.Build.Extensions
{
    internal static class StringExtensions
    {
        public static TextSpan Underline(this string value) =>
            new ContainerSpan(StyleSpan.UnderlinedOn(),
                              new ContentSpan(value),
                              StyleSpan.UnderlinedOff());

        public static TextSpan Rgb(this string value, byte r, byte g, byte b) =>
            new ContainerSpan(ForegroundColorSpan.Rgb(r, g, b),
                              new ContentSpan(value),
                              ForegroundColorSpan.Reset());

        public static TextSpan LightGreen(this string value) =>
            new ContainerSpan(ForegroundColorSpan.LightGreen(),
                              new ContentSpan(value),
                              ForegroundColorSpan.Reset());

        public static TextSpan White(this string value) =>
            new ContainerSpan(ForegroundColorSpan.White(),
                              new ContentSpan(value),
                              ForegroundColorSpan.Reset());
    }
}
