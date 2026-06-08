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

using Akka.Actor;
using Akka.Event;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Neo;

public delegate ILogger LoggerFactory(string source);

/// <summary>
/// Provides a way to configure and get loggers for different sources.
/// If want to set loggers with specific configuration, set LoggerFactory before getting any logger.
/// </summary>
public static class Logs
{
    private static LoggerFactory? s_loggerFactory;

    private static readonly ConcurrentDictionary<string, ILogger> s_loggers = new();

    private static readonly ILogger s_noopLogger = new LoggerConfiguration().CreateLogger();

    public static ILogger RuntimeLogger { get; private set; } = GetLogger("Runtime");

    internal static ILogger AkkaLogger { get; private set; } = GetLogger("Akka");

    /// <summary>
    /// It can been set for creating ILogger with specific configuration.
    /// It should only be set once on startup. If not set, no-op logger will be used.
    /// </summary>
    public static LoggerFactory? LoggerFactory
    {
        get => s_loggerFactory;
        set
        {
            s_loggerFactory = value;
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
        return (s_loggerFactory is null) ? s_noopLogger : s_loggers.GetOrAdd(source, CreateLogger);
    }

    private static ILogger CreateLogger(string source)
    {
        if (s_loggerFactory is null) return s_noopLogger;

        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            source = source.Replace(ch, '-');
        }

        return s_loggerFactory(source);
    }

    internal class LogActor : ReceiveActor
    {
        public LogActor()
        {
            Receive<InitializeLogger>(_ => Sender.Tell(new LoggerInitialized()));
            Receive<Akka.Event.LogEvent>(Log);
        }

        private static void Log(Akka.Event.LogEvent e)
        {
            AkkaLogger.Write(ToLogEventLevel(e.LogLevel()), e.Cause,
                "LogEvent {Message} from {Thread}:{LogSource}", e.Message, e.Thread.Name, e.LogSource);
        }

        internal static LogEventLevel ToLogEventLevel(Akka.Event.LogLevel level) => level switch
        {
            Akka.Event.LogLevel.DebugLevel => LogEventLevel.Debug,
            Akka.Event.LogLevel.InfoLevel => LogEventLevel.Information,
            Akka.Event.LogLevel.WarningLevel => LogEventLevel.Warning,
            Akka.Event.LogLevel.ErrorLevel => LogEventLevel.Error,
            _ => LogEventLevel.Information,
        };
    }
}
