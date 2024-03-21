// Copyright (C) 2015-2024 The Neo Project.
//
// StandardStreamWriterExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine.IO;

namespace Neo.CommandLine.Extensions
{
    internal static class StandardStreamWriterExtensions
    {
        public static void WriteLine(this IStandardStreamWriter writer, string value, AnsiColor textColor, AnsiBackgroundColor bgColor = AnsiBackgroundColor.Default, AnsiStyle textStyle = AnsiStyle.Default)
        {
            writer.WriteLine($"\x1b[{textStyle:d};{textColor:d};{bgColor:d}m{value}\x1b[0m");
        }
    }
}
