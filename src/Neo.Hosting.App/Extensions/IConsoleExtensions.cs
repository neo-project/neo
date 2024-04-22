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
using System.Security;

namespace Neo.Hosting.App.Extensions
{
    internal static class IConsoleExtensions
    {
        private static readonly bool s_colorsAreSupported = GetColorsAreSupported();

        public static void SetTerminalForegroundRed(this IConsole _)
        {
            if (s_colorsAreSupported)
            {
                SetTerminalForegroundColor(_, ConsoleColor.Red);
            }
        }

        public static void SetTerminalForegroundColor(this IConsole _, ConsoleColor consoleColor)
        {
            if (s_colorsAreSupported)
            {
                Console.ForegroundColor = consoleColor;
            }
        }

        public static void SetTerminalBackgroundColor(this IConsole _, ConsoleColor consoleColor)
        {
            if (s_colorsAreSupported)
            {
                Console.BackgroundColor = consoleColor;
            }
        }

        public static void ResetTerminalForegroundColor(this IConsole _)
        {
            if (s_colorsAreSupported)
            {
                Console.ResetColor();
            }
        }

        public static void Clear(this IConsole _) =>
            Console.Clear();

        public static void ErrorMessage(this IConsole _, Exception exception)
        {
            ResetTerminalForegroundColor(_);
            SetTerminalForegroundRed(_);

            var stackTrace = exception.InnerException?.StackTrace ?? exception.StackTrace;

            Console.Error.WriteLine("Exception: ");
            Console.Error.WriteLine("   {0}", exception.InnerException?.Message ?? exception.Message);
            Console.Error.WriteLine("Stack Trace: ");
            Console.Error.WriteLine("   {0}", stackTrace!.Trim());

            ResetTerminalForegroundColor(_);
        }

        public static void ErrorMessage(this IConsole _, string message)
        {
            ResetTerminalForegroundColor(_);
            SetTerminalForegroundRed(_);

            Console.Error.WriteLine($"Error: {message}");

            ResetTerminalForegroundColor(_);
        }

        public static SecureString PromptPassword(this IConsole _)
        {
            ConsoleKeyInfo userInputKeyInfo;
            var userPassword = new SecureString();

            Console.Write("Enter password: ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.CursorVisible = false;

            while ((userInputKeyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (userInputKeyInfo.Key == ConsoleKey.Backspace &&
                    userPassword.Length > 0)
                {
                    userPassword.RemoveAt(userPassword.Length - 1);
                    Console.Write("\b \b");
                }
                else if (char.IsControl(userInputKeyInfo.KeyChar) == false)
                {
                    userPassword.AppendChar(userInputKeyInfo.KeyChar);
                    Console.Write("*");
                }
            }

            userPassword.MakeReadOnly();

            Console.ResetColor();
            Console.CursorVisible = true;
            Console.WriteLine();

            return userPassword;
        }

        public static string? ReadLine(this IConsole _) =>
            Console.ReadLine();

        internal static bool GetColorsAreSupported()
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
