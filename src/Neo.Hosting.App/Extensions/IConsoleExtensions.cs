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
        private static readonly string s_validateInputCharacters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
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

        public static void ErrorWriteLine(this IConsole _, string message)
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

            Console.Write("Password: ");

            while ((userInputKeyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (s_validateInputCharacters.IndexOf(userInputKeyInfo.KeyChar) != -1)
                {
                    userPassword.AppendChar(userInputKeyInfo.KeyChar);
                    Console.Write('*');
                }
                else if (userInputKeyInfo.Key == ConsoleKey.Backspace &&
                    userPassword.Length > 0)
                {
                    userPassword.RemoveAt(userPassword.Length - 1);
                    Console.Write("\b \b");
                }
            }

            userPassword.MakeReadOnly();
            Console.WriteLine();
            return userPassword;
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
