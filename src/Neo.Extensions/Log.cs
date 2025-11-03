// Copyright (C) 2015-2025 The Neo Project.
//
// Log.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Neo
{
    public class Log
    {
        private static readonly string DefaultLogDirectory = Path.Combine(Environment.CurrentDirectory, "Logs");

        private static string? s_logDirectory;

        // For compatibility with the old Log api,
        private static readonly ConcurrentDictionary<string, ILogger> s_loggers = new();


        /// <summary>
        /// The directory where the logs are stored. It only can be set once on startup.
        /// </summary>
        public static string LogDirectory
        {
            get => s_logDirectory ?? DefaultLogDirectory;
            set
            {
                if (s_logDirectory is not null)
                    throw new InvalidOperationException("LogDirectory is already set");
                s_logDirectory = value;
            }
        }

        public static ILogger GetLogger(string source) => s_loggers.GetOrAdd(source, CreateLogger);

        public static ILogger CreateLogger(string source)
        {
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

        public static ILogger ConsoleLogger => new LoggerConfiguration().WriteTo.Console().CreateLogger();
    }
}
