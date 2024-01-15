// Copyright (C) 2015-2024 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo.Node.Service;
using System;

var builder = Host.CreateDefaultBuilder(args);

if (OperatingSystem.IsLinux())
    builder = builder.UseSystemd();
else if (OperatingSystem.IsWindows())
#pragma warning disable CA1416 // Validate platform compatibility
    builder = builder.ConfigureLogging(c => c.AddEventLog())
        .UseWindowsService();
#pragma warning restore CA1416 // Validate platform compatibility


var host = builder
    .ConfigureServices(services =>
    {
        services.AddHostedService<NeoCliPipeService>();
    })
    .Build();

host.Run();
