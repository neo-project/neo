// Copyright (C) 2015-2024 The Neo Project.
//
// AnsiConsole.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.CommandLine
{
    internal delegate void AnsiConsoleKeyEventHandler(object sender, AnsiConsoleKeyEventArgs e);

    internal static class AnsiConsole
    {
        public static event AnsiConsoleKeyEventHandler? KeyPress;

        public static int WindowHeight => Console.WindowHeight;
        public static int WindowWidth => Console.WindowWidth;

        static AnsiConsole()
        {
            ConsoleUtilities.EnableAnsi();
            Console.Clear();
            Console.TreatControlCAsInput = true;
            Console.SetBufferSize(WindowWidth, WindowHeight);
        }

        public static void Write(string format, params object?[] args) =>
            Console.Write(format, args);

        public static void WriteLine(string format, params object?[] args) =>
            Console.WriteLine(format, args);

        public static string? ReadLine() =>
            Console.ReadLine();

        public static char Read() =>
            Convert.ToChar(Console.Read());
    }
}
