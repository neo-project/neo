// Copyright (C) 2015-2024 The Neo Project.
//
// AnsiColorsAndStyles.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.CommandLine.Rendering
{
    internal enum AnsiColors : int
    {
        Black = 30,
        Red = 31,
        Green = 32,
        Yellow = 33,
        Blue = 34,
        Purple = 35,
        Cyan = 36,
        White = 37,
        Default = 39,
        BrightBlack = Black + 60,
        BrightRed = Red + 60,
        BrightGreen = Green + 60,
        BrightYellow = Yellow + 60,
        BrightBlue = Blue + 60,
        BrightPurple = Purple + 60,
        BrightCyan = Cyan + 60,
        BrightWhite = White + 60,
    }

    internal enum AnsiBackgroundColors : int
    {
        Black = 40,
        Red = 41,
        Green = 42,
        Yellow = 43,
        Blue = 44,
        Purple = 45,
        Cyan = 46,
        White = 47,
        Default = 49,
        BrightBlack = Black + 60,
        BrightRed = Red + 60,
        BrightGreen = Green + 60,
        BrightYellow = Yellow + 60,
        BrightBlue = Blue + 60,
        BrightPurple = Purple + 60,
        BrightCyan = Cyan + 60,
        BrightWhite = White + 60,
    }

    internal enum AnsiStyles : int
    {
        Default = 0,

        Bold = 1,

        Faint = 2,

        Italic = 3,

        Underline = 4,

        SlowBlink = 5,

        FastBlink = 6, // Only on Windows

        Invert = 7,

        CrossOut = 9,

        DoubleUnderline = 21,

        OverrideBrightness = 22,

        NoItalic = 23,

        NoUnderline = 24,

        NoBlinking = 25,

        NoInvert = 27,

        NoCrossOut = 29,
    }
}
