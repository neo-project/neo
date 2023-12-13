// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Neo.ConsoleService;
using static System.IO.Path;

namespace Neo.CLI
{
    partial class MainService
    {
        private static readonly ConsoleColorSet DebugColor = new(ConsoleColor.Cyan);
        private static readonly ConsoleColorSet InfoColor = new(ConsoleColor.White);
        private static readonly ConsoleColorSet WarningColor = new(ConsoleColor.Yellow);
        private static readonly ConsoleColorSet ErrorColor = new(ConsoleColor.Red);
        private static readonly ConsoleColorSet FatalColor = new(ConsoleColor.Red);

        private readonly object syncRoot = new();
        private bool _showLog = Settings.Default.Logger.ConsoleOutput;

        private void Initialize_Logger()
        {
            Utility.Logging += OnLog;
        }

        private void Dispose_Logger()
        {
            Utility.Logging -= OnLog;
        }

        /// <summary>
        /// Process "console log off" command to turn off console log
        /// </summary>
        [ConsoleCommand("console log off", Category = "Log Commands")]
        private void OnLogOffCommand()
        {
            _showLog = false;
        }

        /// <summary>
        /// Process "console log on" command to turn on the console log
        /// </summary>
        [ConsoleCommand("console log on", Category = "Log Commands")]
        private void OnLogOnCommand()
        {
            _showLog = true;
        }

        private static void GetErrorLogs(StringBuilder sb, Exception ex)
        {
            sb.AppendLine(ex.GetType().ToString());
            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);
            if (ex is AggregateException ex2)
            {
                foreach (Exception inner in ex2.InnerExceptions)
                {
                    sb.AppendLine();
                    GetErrorLogs(sb, inner);
                }
            }
            else if (ex.InnerException != null)
            {
                sb.AppendLine();
                GetErrorLogs(sb, ex.InnerException);
            }
        }

        private void OnLog(string source, LogLevel level, object message)
        {
            if (!Settings.Default.Logger.Active)
                return;

            if (message is Exception ex)
            {
                var sb = new StringBuilder();
                GetErrorLogs(sb, ex);
                message = sb.ToString();
            }

            lock (syncRoot)
            {
                DateTime now = DateTime.Now;
                var log = $"[{now.TimeOfDay:hh\\:mm\\:ss\\.fff}]";
                if (_showLog)
                {
                    var currentColor = new ConsoleColorSet();
                    var messages = message is string msg ? Parse(msg) : new[] { message.ToString() };
                    ConsoleColorSet logColor;
                    string logLevel;
                    switch (level)
                    {
                        case LogLevel.Debug: logColor = DebugColor; logLevel = "DEBUG"; break;
                        case LogLevel.Error: logColor = ErrorColor; logLevel = "ERROR"; break;
                        case LogLevel.Fatal: logColor = FatalColor; logLevel = "FATAL"; break;
                        case LogLevel.Info: logColor = InfoColor; logLevel = "INFO"; break;
                        case LogLevel.Warning: logColor = WarningColor; logLevel = "WARN"; break;
                        default: logColor = InfoColor; logLevel = "INFO"; break;
                    }
                    logColor.Apply();
                    Console.Write($"{logLevel} {log} \t{messages[0],-20}");
                    for (var i = 1; i < messages.Length; i++)
                    {
                        if (messages[i].Length > 20)
                        {
                            messages[i] = $"{messages[i][..10]}...{messages[i][(messages[i].Length - 10)..]}";
                        }
                        Console.Write(i % 2 == 0 ? $"={messages[i]} " : $" {messages[i]}");
                    }
                    currentColor.Apply();
                    Console.WriteLine();
                }

                if (string.IsNullOrEmpty(Settings.Default.Logger.Path)) return;
                var sb = new StringBuilder(source);
                foreach (var c in GetInvalidFileNameChars())
                    sb.Replace(c, '-');
                var path = Combine(Settings.Default.Logger.Path, sb.ToString());
                Directory.CreateDirectory(path);
                path = Combine(path, $"{now:yyyy-MM-dd}.log");
                try
                {
                    File.AppendAllLines(path, new[] { $"[{level}]{log} {message}" });
                }
                catch (IOException)
                {
                    Console.WriteLine("Error writing the log file: " + path);
                }
            }
        }

        /// <summary>
        /// Parse the log message
        /// </summary>
        /// <param name="message">expected format [key1 = msg1 key2 = msg2]</param>
        /// <returns></returns>
        private static string[] Parse(string message)
        {
            var equals = message.Trim().Split('=');

            if (equals.Length == 1) return new[] { message };

            var messages = new List<string>();
            foreach (var t in @equals)
            {
                var msg = t.Trim();
                var parts = msg.Split(' ');
                var d = parts.Take(parts.Length - 1);

                if (parts.Length > 1)
                {
                    messages.Add(string.Join(" ", d));
                }
                messages.Add(parts.LastOrDefault());
            }

            return messages.ToArray();
        }
    }
}
