// Copyright (C) 2015-2024 The Neo Project.
//
// UT_AnsiString.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CommandLine.Rendering;

namespace Neo.CommandLine.Tests
{
    public class UT_AnsiString
    {
        [Fact]
        public void Test_Output_String_Styling()
        {
            var result = new AnsiString("Hello World", new()
            {
                Color = AnsiColor.Green,
                Background = AnsiBackgroundColor.White,
                Style = AnsiStyle.Underline,
            });

            Assert.Equal("\x1b[4;32;47mHello World", result);
        }
    }
}
