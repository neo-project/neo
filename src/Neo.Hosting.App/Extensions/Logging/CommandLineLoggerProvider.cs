// Copyright (C) 2015-2024 The Neo Project.
//
// CommandLineLoggerProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.CommandLine.Invocation;

namespace Neo.Hosting.App.Extensions.Logging
{
    [ProviderAlias("CommandLine")]
    internal sealed class CommandLineLoggerProvider
        (InvocationContext context) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, CommandLineLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new CommandLineLogger(context));

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
