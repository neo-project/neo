// Copyright (C) 2015-2024 The Neo Project.
//
// IConsoleExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine;

namespace Neo.Service.App.Extensions
{
    internal static class IConsoleExtensions
    {
        private static readonly bool ColorsAreSupported = GetColorsAreSupported();

        public static void SetTerminalForegroundRed(this IConsole _)
        {
            if (ColorsAreSupported)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
        }

        public static void ResetTerminalForegroundColor(this IConsole _)
        {
            if (ColorsAreSupported)
            {
                Console.ResetColor();
            }
        }

        private static bool GetColorsAreSupported()
#if NET7_0_OR_GREATER
            => !(OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
#else
            => !(RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"))
                    || RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))
                    || RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"))
                    || RuntimeInformation.IsOSPlatform(OSPlatform.Create("TVOS")))
#endif
            && !Console.IsOutputRedirected;
    }
}
