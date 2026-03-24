// Copyright (C) 2015-2026 The Neo Project.
//
// Logs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Serilog;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Neo
{
    public static class Logs
    {
        private static string? s_logDirectory;

        private static readonly ConcurrentDictionary<string, ILogger> s_loggers = new();

        private static readonly ILogger s_noopLogger = new LoggerConfiguration().CreateLogger();

        public static ILogger ConsoleLogger { get; private set; } = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        public static ILogger RuntimeLogger { get; private set; } = GetLogger("Runtime");

        internal static ILogger AkkaLogger { get; private set; } = GetLogger("Akka");

        /// <summary>
        /// The directory where the logs are stored. If not set, the logs will be disabled.
        /// It only can be set once on startup.
        /// </summary>
        public static string? LogDirectory
        {
            get => s_logDirectory;
            set
            {
                if (s_logDirectory is not null) // cannot be changed after setup
                    throw new InvalidOperationException("LogDirectory is already set");
                s_logDirectory = value;
                RuntimeLogger = GetLogger("Runtime");
                AkkaLogger = GetLogger("Akka");
            }
        }

        /// <summary>
        /// Get a logger for the given source. If the log directory is not set, a no-op logger will be returned.
        /// If want to set the log directory, set it before calling this method.
        /// </summary>
        /// <param name="source">The source of the log.</param>
        /// <returns>A logger for the given source.</returns>
        public static ILogger GetLogger(string source)
        {
            return (LogDirectory is null) ? s_noopLogger : s_loggers.GetOrAdd(source, CreateLogger);
        }

        private static ILogger CreateLogger(string source)
        {
            if (LogDirectory is null) return s_noopLogger;

            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                source = source.Replace(ch, '-');
            }

            return new LoggerConfiguration()
                .WriteTo.File(
                    path: Path.Combine(LogDirectory, source, "log-.txt"),
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100 MiB
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30 // about 1 month
                )
                .CreateLogger();
        }

        public static LogEventLevel ToLogEventLevel(this LogLevel level) => level switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Info => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Fatal => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };
    }
}
