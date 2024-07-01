// Copyright (C) 2015-2024 The Neo Project.
//
// CommandLineLogger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine.Invocation;

namespace Neo.Hosting.App.Extensions.Logging
{
    using Microsoft.Extensions.Logging;

    internal sealed class CommandLineLogger
        (InvocationContext context) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            default!;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    context.Console.ErrorMessage(formatter(state, exception));
                    break;
                case LogLevel.Warning:
                    context.Console.WarningMessage(formatter(state, exception));
                    break;
                case LogLevel.Information:
                    context.Console.InfoMessage(formatter(state, exception));
                    break;
                case LogLevel.Debug:
                    context.Console.DebugMessage(formatter(state, exception));
                    break;
                case LogLevel.Trace:
                    context.Console.TraceMessage(formatter(state, exception));
                    break;
                default:
                    context.Console.WriteLine(formatter(state, exception));
                    break;
            }
        }
    }
}
