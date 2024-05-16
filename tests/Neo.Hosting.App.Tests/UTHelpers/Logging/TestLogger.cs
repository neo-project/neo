// Copyright (C) 2015-2024 The Neo Project.
//
// UT_XUnitLogger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo;
using Neo.Hosting;
using Neo.Hosting.App;
using Neo.Hosting.App.Tests;
using Neo.Hosting.App.Tests.UTHelpers;
using Neo.Hosting.App.Tests.UTHelpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.UTHelpers.Logging
{
    public sealed class TestLogger
        (ITestOutputHelper testOutputHelper, string categoryName) : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
        private readonly string _categoryName = categoryName;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            default!;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
            true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _testOutputHelper.WriteLine($"[{DateTime.Now}] {logLevel}: {_categoryName}[{eventId.Id}] {formatter(state, exception)}");
    }
}
