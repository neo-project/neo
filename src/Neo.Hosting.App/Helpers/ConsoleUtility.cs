// Copyright (C) 2015-2024 The Neo Project.
//
// ConsoleUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.InteropServices;

namespace Neo.Hosting.App
{
    internal static partial class ConsoleUtility
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 8;

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial nint GetStdHandle(int nStdHandle);

        public static void EnableAnsi()
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);

            if (!GetConsoleMode(handle, out var mode))
            {
                Console.Error.WriteLine("Failed to get console mode");
                return;
            }

            // Enable the virtual terminal processing mode
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(handle, mode))
            {
                Console.Error.WriteLine("Failed to set console mode");
                return;
            }
        }
    }
}
