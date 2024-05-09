// Copyright (C) 2015-2024 The Neo Project.
//
// UT_TestLoggerFixture.cs file belongs to the neo project and is free
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
using Neo.Hosting.App.Tests.UTHelpers.SetupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.UTHelpers.SetupClasses
{
    public class UT_SetupTestLogging : IDisposable
    {
        private readonly UT_TestTextWriter _logConsole;

        public ILoggerFactory LoggerFactory { get; }

        public UT_SetupTestLogging(
            ITestOutputHelper testOutputHelper)
        {
            _logConsole = new UT_TestTextWriter(testOutputHelper);
            Console.SetOut(_logConsole);

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new UT_XUnitLoggerProvider(testOutputHelper));
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            });
        }

        public void Dispose()
        {
            _logConsole.Dispose();
        }
    }
}
