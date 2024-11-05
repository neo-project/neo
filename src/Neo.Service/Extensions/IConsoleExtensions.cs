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

using Neo.Service.CommandLine;
using System;
using System.CommandLine;
using System.CommandLine.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Service.Extensions
{
    internal static class IConsoleExtensions
    {
        public static void SetTerminalForegroundColor(this IConsole console, ConsoleColor consoleColor)
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

        public static void SetTerminalBackgroundColor(this IConsole console, ConsoleColor consoleColor)
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

        public static void ResetColor(this IConsole console) =>
            console.Write($"{Ansi.Color.Foreground.Default}{Ansi.Color.Background.Default}");

        public static void Clear(this IConsole console) =>
            console.Write($"{Ansi.Clear.EntireScreen}{Ansi.Cursor.Move.ToUpperLeftCorner}");

        public static void SetCursorPosition(this IConsole console, int left, int right) =>
            console.Write($"{Ansi.Cursor.Move.ToLocation(left, right)}");

        public static void WriteLine(this IConsole console) =>
            console.Write(Environment.NewLine);

        public static void WriteLine(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string value, params object[] args)
        {
            console.Write(string.Format(value, args) + Environment.NewLine);
        }

        public static void InfoMessage(this IConsole console, string message)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Blue);
            console.Write("Info: ");
            console.SetTerminalForegroundColor(ConsoleColor.DarkBlue);
            console.WriteLine(message);
            console.ResetColor();
        }

        public static void WarningMessage(this IConsole console, string message)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Yellow);
            console.Write("Warning: ");
            console.SetTerminalForegroundColor(ConsoleColor.DarkYellow);
            console.WriteLine(message);
            console.ResetColor();
        }

        public static void DebugMessage(this IConsole console, string message)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Gray);
            console.WriteLine(message);
            console.ResetColor();
        }

        public static void TraceMessage(this IConsole console, string message)
        {
            console.SetTerminalForegroundColor(ConsoleColor.DarkGray);
            console.WriteLine(message);
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

        public static void ErrorMessage(this IConsole console, string message)
        {
            console.SetTerminalForegroundColor(ConsoleColor.Red);
            console.Write("Error: ");
            console.SetTerminalForegroundColor(ConsoleColor.DarkRed);
            console.WriteLine(message);
            console.ResetColor();
        }

        public static ConsoleInput PromptConfirm(this IConsole console, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object[] args)
        {
            var isRedirect = console.IsInputRedirected;

            if (isRedirect == false)
            {
                console.SetTerminalForegroundColor(ConsoleColor.Blue);
                console.Write(string.Format(format, args));
                console.SetTerminalForegroundColor(ConsoleColor.Yellow);
                console.Write("  [Y]es / [N]o / [C]ancel :  ");
                console.ResetColor();
            }

            var answer = console.ReadLine();

            if (answer == null)
                return ConsoleInput.Empty;
            else if (answer.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
                answer.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                return ConsoleInput.Yes;
            else if (answer.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
                answer.Equals("n", StringComparison.InvariantCultureIgnoreCase))
                return ConsoleInput.No;
            else if (answer.Equals("cancel", StringComparison.InvariantCultureIgnoreCase) ||
                answer.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                return ConsoleInput.Cancel;
            else
                return console.PromptConfirm(format, args);
        }

        public static string? PromptPassword(this IConsole console)
        {
            var isRedirect = console.IsInputRedirected;

            if (isRedirect == false)
            {
                console.SetTerminalForegroundColor(ConsoleColor.White);
                console.Write("Enter password: ");
                console.Write($"{Ansi.Text.HiddenOn}");
                console.Write($"{Ansi.Cursor.SavePosition}");
            }

            var userPassword = console.ReadLine();
            console.Write($"{Ansi.Cursor.RestorePosition}{Ansi.Clear.ToEndOfLine}");
            console.Write($"{Ansi.Text.AttributesOff}");
            console.ResetColor();
            console.WriteLine();

            return userPassword;
        }

        public static string? ReadLine(this IConsole console) =>
            (console as NamedPipeConsole)?.ReadLine();

        public static int Read(this IConsole console) =>
            (console as NamedPipeConsole)?.Read() ?? 0;
    }
}
