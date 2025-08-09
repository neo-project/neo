// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildConsoleLoggerProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.ToolSet.Logging;
using System.CommandLine;

namespace Neo.Build.ToolSet.Providers
{
    [ProviderAlias("NeoBuild")]
    internal class NeoBuildConsoleLoggerProvider(IConsole console) : ILoggerProvider
    {
        private readonly IConsole _console = console;

        public ILogger CreateLogger(string name) =>
            new NeoBuildConsoleLogger(name, _console);

        public void Dispose() { }
    }
}
