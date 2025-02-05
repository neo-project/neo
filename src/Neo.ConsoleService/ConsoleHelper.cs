// Copyright (C) 2015-2025 The Neo Project.
//
// ConsoleHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Security;
using System.Text;

namespace Neo.ConsoleService
{
    public static class ConsoleHelper
    {
        private static readonly ConsoleColorSet InfoColor = new(ConsoleColor.Cyan);
        private static readonly ConsoleColorSet WarningColor = new(ConsoleColor.Yellow);
        private static readonly ConsoleColorSet ErrorColor = new(ConsoleColor.Red);

        public static bool ReadingPassword { get; private set; } = false;

        /// <summary>
        /// Info handles message in the format of "[tag]:[message]",
        /// avoid using Info if the `tag` is too long
        /// </summary>
        /// <param name="values">The log message in pairs of (tag, message)</param>
        public static void Info(params string[] values)
        {
            var currentColor = new ConsoleColorSet();

            for (int i = 0; i < values.Length; i++)
            {
                if (i % 2 == 0)
                    InfoColor.Apply();
                else
                    currentColor.Apply();
                Console.Write(values[i]);
            }
            currentColor.Apply();
            Console.WriteLine();
        }

        /// <summary>
        /// Use warning if something unexpected happens
        /// or the execution result is not correct.
        /// Also use warning if you just want to remind
        /// user of doing something.
        /// </summary>
        /// <param name="msg">Warning message</param>
        public static void Warning(string msg)
        {
            Log("Warning", WarningColor, msg);
        }

        /// <summary>
        /// Use Error if the verification or input format check fails
        /// or exception that breaks the execution of interactive
        /// command throws.
        /// </summary>
        /// <param name="msg">Error message</param>
        public static void Error(string msg)
        {
            Log("Error", ErrorColor, msg);
        }

        private static void Log(string tag, ConsoleColorSet colorSet, string msg)
        {
            var currentColor = new ConsoleColorSet();

            colorSet.Apply();
            Console.Write($"{tag}: ");
            currentColor.Apply();
            Console.WriteLine(msg);
        }

        public static string ReadUserInput(string prompt, bool password = false)
        {
            const string t = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write(prompt + ": ");
            }

            if (password) ReadingPassword = true;
            var prevForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            if (Console.IsInputRedirected)
            {
                // neo-gui Console require it
                sb.Append(Console.ReadLine());
            }
            else
            {
                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);

                    if (t.IndexOf(key.KeyChar) != -1)
                    {
                        sb.Append(key.KeyChar);
                        Console.Write(password ? '*' : key.KeyChar);
                    }
                    else if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                } while (key.Key != ConsoleKey.Enter);
            }

            Console.ForegroundColor = prevForeground;
            if (password) ReadingPassword = false;
            Console.WriteLine();
            return sb.ToString();
        }

        public static SecureString ReadSecureString(string prompt)
        {
            const string t = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            SecureString securePwd = new SecureString();
            ConsoleKeyInfo key;

            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write(prompt + ": ");
            }

            ReadingPassword = true;
            Console.ForegroundColor = ConsoleColor.Yellow;

            do
            {
                key = Console.ReadKey(true);
                if (t.IndexOf(key.KeyChar) != -1)
                {
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write('*');
                }
                else if (key.Key == ConsoleKey.Backspace && securePwd.Length > 0)
                {
                    securePwd.RemoveAt(securePwd.Length - 1);
                    Console.Write(key.KeyChar);
                    Console.Write(' ');
                    Console.Write(key.KeyChar);
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.ForegroundColor = ConsoleColor.White;
            ReadingPassword = false;
            Console.WriteLine();
            securePwd.MakeReadOnly();
            return securePwd;
        }
    }
}
