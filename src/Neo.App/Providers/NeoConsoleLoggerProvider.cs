// Copyright (C) 2015-2025 The Neo Project.
//
// NeoConsoleLoggerProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.App.Logging;
using System.CommandLine;

namespace Neo.App.Providers
{
    [ProviderAlias("NeoConsole")]
    internal class NeoConsoleLoggerProvider(IConsole console) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) =>
            new NeoConsoleLogger(categoryName, console);

        public void Dispose() { }
    }
}
