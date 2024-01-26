// Copyright (C) 2015-2024 The Neo Project.
//
// UT_XUnitLoggerProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace Neo.Service.Tests.Helpers
{
    public class UT_XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UT_XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
            => new UT_XUnitLogger(_testOutputHelper, categoryName);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class UT_XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public UT_XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => UT_NoOpDisposable.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _testOutputHelper.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");
            if (exception != null)
                _testOutputHelper.WriteLine(exception.ToString());
        }

        private class UT_NoOpDisposable : IDisposable
        {
            public static readonly UT_NoOpDisposable Instance = new UT_NoOpDisposable();

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
