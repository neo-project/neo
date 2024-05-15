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

        public static void ResetColor(this IConsole _)
        {
            if (s_colorsAreSupported)
            {
                Console.ResetColor();
            }
        }

        public static void Clear(this IConsole _) =>
            Console.Clear();

        public static void WriteLine(this IConsole _) =>
            Console.WriteLine();

        public static void Write(this IConsole _, string value, params object[] args) =>
            Console.Write(value, args);

        public static void InfoMessage(this IConsole console, string message)
        {
            console.ResetColor();
            console.SetTerminalForegroundColor(ConsoleColor.DarkMagenta);
            console.Write("Info: ");
            console.SetTerminalForegroundColor(ConsoleColor.White);
            console.WriteLine(message);

            console.ResetColor();
        }

        public static void ErrorMessage(this IConsole console, Exception exception)
        {
            console.ResetColor();
            console.SetTerminalForegroundColor(ConsoleColor.Red);

            var stackTrace = exception.InnerException?.StackTrace ?? exception.StackTrace;

            Console.Error.WriteLine("Exception: ");
            Console.Error.WriteLine("   {0}", exception.InnerException?.Message ?? exception.Message);
            Console.Error.WriteLine("Stack Trace: ");
            Console.Error.WriteLine("   {0}", stackTrace?.Trim());

            console.ResetColor();
        }

        public static void ErrorMessage(this IConsole console, string message)
        {
            console.ResetColor();
            console.SetTerminalForegroundColor(ConsoleColor.Red);

            Console.Error.WriteLine($"Error: {message}");

            console.ResetColor();
        }

        public static SecureString PromptPassword(this IConsole console)
        {
            ConsoleKeyInfo userInputKeyInfo;
            var userPassword = new SecureString();

            var isRedirect = Console.IsInputRedirected;

            if (isRedirect == false)
            {
                console.Write("Enter password: ");
                SetTerminalForegroundColor(console, ConsoleColor.DarkYellow);
            }

            while ((userInputKeyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (userInputKeyInfo.Key == ConsoleKey.Backspace &&
                    userPassword.Length > 0)
                {
                    userPassword.RemoveAt(userPassword.Length - 1);

                    if (isRedirect == false)
                        console.Write("\b \b");
                }
                else if (char.IsControl(userInputKeyInfo.KeyChar) == false)
                {
                    userPassword.AppendChar(userInputKeyInfo.KeyChar);

                    if (isRedirect)
                        console.Write("**");
                }
            }

            userPassword.MakeReadOnly();

            console.ResetColor();
            console.WriteLine();

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
