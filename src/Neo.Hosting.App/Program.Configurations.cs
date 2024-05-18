// Copyright (C) 2015-2024 The Neo Project.
//
// Program.Configurations.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.EventLog;
using Neo.Hosting.App.Extensions;
using System;

namespace Neo.Hosting.App
{
    public partial class Program
    {
        internal static IHostBuilder DefaultNeoHostBuilderFactory(string[] args) =>
            new HostBuilder()
            .UseNeoHostConfiguration()
            .UseNeoAppConfiguration()
            .ConfigureServices(AddDefaultServices)
            .UseServiceProviderFactory((context) => new DefaultServiceProviderFactory(CreateDefaultNeoServiceProviderOptions(context)));

        static ServiceProviderOptions CreateDefaultNeoServiceProviderOptions(HostBuilderContext context)
        {
            var flag = context.HostingEnvironment.IsNeoLocalNet();
            return new ServiceProviderOptions
            {
                ValidateScopes = flag,
                ValidateOnBuild = flag
            };
        }

        static void AddDefaultServices(HostBuilderContext hostingContext, IServiceCollection services)
        {
            services.AddLogging(logging =>
            {
                logging.ClearProviders();

                var isWindows = OperatingSystem.IsWindows();
                if (isWindows && hostingContext.HostingEnvironment.IsNeoLocalNet() == false)
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= Microsoft.Extensions.Logging.LogLevel.Warning);

#if DEBUG
                logging.AddFilter<DebugLoggerProvider>(level => level >= Microsoft.Extensions.Logging.LogLevel.Trace);
                logging.AddDebug();
#endif

                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddEventSourceLogger();

                if (IsRunningAsService == false)
                {
                    logging.AddSimpleConsole(config =>
                    {
                        config.ColorBehavior = LoggerColorBehavior.Enabled;
                        config.SingleLine = true;
                        config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
                        config.UseUtcTimestamp = true;
                    });
                }

                // Adds Neo File Logger: outputs to "./logs/"
                logging.AddNeoFileLogger();

                if (isWindows)
                    logging.AddEventLog();

                logging.Configure(options =>
                {
                    options.ActivityTrackingOptions =
                        ActivityTrackingOptions.SpanId |
                        ActivityTrackingOptions.TraceId |
                        ActivityTrackingOptions.ParentId;
                });
            });
        }
    }
}
