// Copyright (C) 2015-2025 The Neo Project.
//
// NeoConsoleLogger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.App.Extensions;
using System;
using System.CommandLine;

namespace Neo.App.Logging
{
    internal class NeoConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly IConsole _console;

        public NeoConsoleLogger(string categoryName, IConsole console)
        {
            _name = categoryName;
            _console = console;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            default!;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
            logLevel != Microsoft.Extensions.Logging.LogLevel.None;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) == false)
                return;

            ArgumentNullException.ThrowIfNull(formatter);

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
                return;

            message = $"{_name}[{eventId.Id}] {message}";

            if (exception is not null)
            {
                message += Environment.NewLine + Environment.NewLine + exception;
            }

            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    _console.TraceMessage("{0}", message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    _console.DebugMessage("{0}", message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    _console.InfoMessage("{0}", message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    _console.WarningMessage("{0}", message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    _console.ErrorMessage("{0}", message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    _console.ErrorMessage("{0}", message);
                    break;
                default:
                    break;
            }
        }
    }
}
