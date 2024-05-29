// Copyright (C) 2015-2024 The Neo Project.
//
// TestSetupIHost.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo;
using Neo.Hosting;
using Neo.Hosting.App;
using Neo.Hosting.App.Extensions;
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
using Xunit.Sdk;

namespace Neo.Hosting.App.Tests.UTHelpers.SetupClasses
{
    public class TestSetupIHost
    {
        public IHost Host { get; private init; }

        public TestSetupIHost(
            ITestOutputHelper testOutputHelper)
        {
            Host = Program.DefaultNeoHostBuilderFactory(null)
                .UseNeoServiceConfiguration()
                .ConfigureLogging(builder =>
                {
                    builder.AddProvider(new TestLoggerProvider(testOutputHelper));
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .Build();
        }
    }
}
