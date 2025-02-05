// Copyright (C) 2015-2025 The Neo Project.
//
// ConsoleExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine;
using System.CommandLine.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Build.Extensions
{
    internal static class ConsoleExtensions
    {
        private static readonly bool s_colorsAreSupported = GetColorsAreSupported();

        public static void SetTerminalForegroundColor(this IConsole console, ConsoleColor consoleColor)
        {
            if (s_colorsAreSupported)
            {
                var colorString = consoleColor switch
                {
                    ConsoleColor.Black => $"{Ansi.Color.Foreground.Black}",
                    ConsoleColor.DarkBlue => $"{Ansi.Color.Foreground.Blue}",
                    ConsoleColor.DarkGreen => $"{Ansi.Color.Foreground.Green}",
                    ConsoleColor.DarkCyan => $"{Ansi.Color.Foreground.Cyan}",
                    ConsoleColor.DarkRed => $"{Ansi.Color.Foreground.Red}",
                    ConsoleColor.DarkMagenta => $"{Ansi.Color.Foreground.Magenta}",
                    ConsoleColor.DarkYellow => $"{Ansi.Color.Foreground.Yellow}",
                    ConsoleColor.Gray => $"{Ansi.Color.Foreground.LightGray}",
                    ConsoleColor.DarkGray => $"{Ansi.Color.Foreground.DarkGray}",
                    ConsoleColor.Blue => $"{Ansi.Color.Foreground.LightBlue}",
                    ConsoleColor.Green => $"{Ansi.Color.Foreground.LightGreen}",
                    ConsoleColor.Cyan => $"{Ansi.Color.Foreground.LightCyan}",
                    ConsoleColor.Red => $"{Ansi.Color.Foreground.LightRed}",
                    ConsoleColor.Magenta => $"{Ansi.Color.Foreground.LightMagenta}",
                    ConsoleColor.Yellow => $"{Ansi.Color.Foreground.LightYellow}",
                    ConsoleColor.White => $"{Ansi.Color.Foreground.White}",
                    _ => string.Empty,
                };
                console.Write(colorString);
            }
        }

        public static void SetTerminalBackgroundColor(this IConsole console, ConsoleColor consoleColor)
        {
            if (s_colorsAreSupported)
            {
                var colorString = consoleColor switch
                {
                    ConsoleColor.Black => $"{Ansi.Color.Background.Black}",
                    ConsoleColor.DarkBlue => $"{Ansi.Color.Background.Blue}",
                    ConsoleColor.DarkGreen => $"{Ansi.Color.Background.Green}",
                    ConsoleColor.DarkCyan => $"{Ansi.Color.Background.Cyan}",
                    ConsoleColor.DarkRed => $"{Ansi.Color.Background.Red}",
                    ConsoleColor.DarkMagenta => $"{Ansi.Color.Background.Magenta}",
                    ConsoleColor.DarkYellow => $"{Ansi.Color.Background.Yellow}",
                    ConsoleColor.Gray => $"{Ansi.Color.Background.LightGray}",
                    ConsoleColor.DarkGray => $"{Ansi.Color.Background.DarkGray}",
                    ConsoleColor.Blue => $"{Ansi.Color.Background.LightBlue}",
                    ConsoleColor.Green => $"{Ansi.Color.Background.LightGreen}",
                    ConsoleColor.Cyan => $"{Ansi.Color.Background.LightCyan}",
                    ConsoleColor.Red => $"{Ansi.Color.Background.LightRed}",
                    ConsoleColor.Magenta => $"{Ansi.Color.Background.LightMagenta}",
                    ConsoleColor.Yellow => $"{Ansi.Color.Background.LightYellow}",
                    ConsoleColor.White => $"{Ansi.Color.Background.White}",
                    _ => string.Empty,
                };
                console.Write(colorString);
            }
        }

        public static void ResetColor(this IConsole console) =>
            console.Write($"{Ansi.Color.Foreground.Default}{Ansi.Color.Background.Default}");

        public static void WriteLine(this IConsole console) =>
            console.Write(Environment.NewLine);

        public static void WriteLine(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            console.Write(string.Format(format, args) + Environment.NewLine);
        }

        public static void InfoMessage(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Blue);
            console.Write($"[Info] ");
            console.SetTerminalForegroundColor(ConsoleColor.White);
            console.WriteLine(format, args);
            console.ResetColor();
        }

        public static void WarningMessage(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Yellow);
            console.Write("[Warn] ");
            console.SetTerminalForegroundColor(ConsoleColor.White);
            console.WriteLine(format, args);
            console.ResetColor();
        }

        public static void DebugMessage(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            console.SetTerminalForegroundColor(ConsoleColor.DarkGray);
            console.Write("[Debug] ");
            console.SetTerminalForegroundColor(ConsoleColor.DarkGray);
            console.WriteLine(format, args);
            console.ResetColor();
        }

        public static void TraceMessage(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            console.SetTerminalForegroundColor(ConsoleColor.DarkGray);
            console.Write("[Trace] ");
            console.SetTerminalForegroundColor(ConsoleColor.DarkGray);
            console.WriteLine(format, args);
            console.ResetColor();
        }

        public static void ErrorMessage(this IConsole console, Exception exception, bool showStackTrace = true)
        {
            var stackTrace = exception.InnerException?.StackTrace ?? exception.StackTrace;

            console.SetTerminalForegroundColor(ConsoleColor.Red);
            console.WriteLine("{0}: ", exception.InnerException?.GetType().Name ?? exception.GetType().Name);
            console.SetTerminalForegroundColor(ConsoleColor.DarkRed);
            console.WriteLine("   {0}", exception.InnerException?.Message ?? exception.Message);
            console.SetTerminalForegroundColor(ConsoleColor.Red);

            if (showStackTrace)
            {
                console.WriteLine("Stack Trace: ");
                console.SetTerminalForegroundColor(ConsoleColor.DarkRed);
                console.WriteLine("   {0}", stackTrace?.Trim() ?? string.Empty);
            }

            console.ResetColor();
        }

        public static void ErrorMessage(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Red);
            console.Write("Error: ");
            console.SetTerminalForegroundColor(ConsoleColor.DarkRed);
            console.WriteLine(format, args);
            console.ResetColor();
        }

        internal static bool GetColorsAreSupported() =>
            !Console.IsOutputRedirected;
    }
}
