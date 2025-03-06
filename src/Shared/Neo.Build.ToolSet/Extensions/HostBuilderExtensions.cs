// Copyright (C) 2015-2025 The Neo Project.
//
// HostBuilderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;

namespace Neo.Build.ToolSet.Extensions
{
    internal static class HostBuilderExtensions
    {
        public static IHostBuilder UseNeoBuildConfiguration(this IHostBuilder hostBuilder)
        {
            // Host Configuration
            hostBuilder.ConfigureHostConfiguration(config =>
            {
                var manger = new ConfigurationManager();

                config.AddConfiguration(manger);
                config.AddNeoBuildConfiguration();
            });

            // Application Configuration
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                var environmentName = context.HostingEnvironment.EnvironmentName;

                config.AddJsonFile($"config.{environmentName}.json", optional: true);
            });

            // Logging Configuration
            hostBuilder.ConfigureLogging((context, logging) =>
            {
                var isWindows = OperatingSystem.IsWindows();

                if (isWindows)
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= Microsoft.Extensions.Logging.LogLevel.Warning);

                logging.AddConfiguration(context.Configuration.GetSection("Logging"));

                logging.AddDebug();
                logging.AddEventSourceLogger();

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

            hostBuilder.UseDefaultServiceProvider((context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsLocalnet();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            hostBuilder.ConfigureServices(services =>
            {
                // Add Services Here
            });

            return hostBuilder;
        }
    }
}
