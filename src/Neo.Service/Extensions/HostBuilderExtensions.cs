// Copyright (C) 2015-2024 The Neo Project.
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Neo.IO.Pipes;
using Neo.Service.Configuration;
using Neo.Service.Hosting;
using Neo.Service.Hosting.Services;
using Neo.Service.Pipes;
using System;
using System.IO;

namespace Neo.Service.Extensions
{
    internal static class HostBuilderExtensions
    {
        /// <summary>
        /// Configurations for running the hosting environment.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseNeoHostConfiguration(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureHostConfiguration(config =>
            {
                config.AddNeoConfiguration();
            });

            return hostBuilder;
        }

        /// <summary>
        /// Configurations for running the application.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseNeoAppConfiguration(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration(config =>
            {
                config.AddNeoConfiguration();
                config.AddNeoDefaultFiles();
            });

            return hostBuilder;
        }

        /// <summary>
        /// Sets up and injects <see cref="NeoOptions"/> into services.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseNeoConfigFile(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.ConfigureOptions<NeoOptionsSetup>();
            });
            return hostBuilder;
        }

        public static IHostBuilder UseNamedPipes(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var pipeName = context.Configuration
                    .GetSection(NeoConfigurationSectionNames.ApplicationConfiguration)
                    .GetSection("Service")
                    .GetValue<string>("PipeName");
                var endPoint = new NamedPipeEndPoint(pipeName ?? NeoDefaults.PipeName);

                services.TryAddSingleton(endPoint);
                services.TryAddSingleton<NamedPipeListener>();
                services.AddHostedService<NamedPipeService>();
            });
            return hostBuilder;
        }

        public static IHostBuilder UseNeoSystem(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var protocolSettingsSection = context.Configuration
                    .GetRequiredSection(NeoConfigurationSectionNames.ProtocolConfiguration);
                var protocolSettings = ProtocolSettings.Load(protocolSettingsSection);

                services.TryAddSingleton(protocolSettings);

                var applicationSection = context.Configuration
                    .GetRequiredSection(NeoConfigurationSectionNames.ApplicationConfiguration)
                    .GetRequiredSection("Storage");
                var storageOptions = applicationSection.Get<StorageOptions>();

                string? storagePath = null;
                if (string.IsNullOrEmpty(storageOptions?.Path) == false)
                {
                    storagePath = string.Format(storageOptions.Path, protocolSettings.Network);
                    if (Directory.Exists(storagePath) == false)
                    {
                        if (Path.IsPathFullyQualified(storagePath) == false)
                            storagePath = Path.Combine(AppContext.BaseDirectory, storagePath);
                    }
                }
                services.TryAddSingleton(new NeoSystem(protocolSettings, storageOptions?.Engine ?? NeoDefaults.StoreProviderName, storagePath));
            });
            return hostBuilder;
        }

        public static IHostBuilder UseDefaultServices(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                //services.Configure<InvocationLifetimeOptions>(config => config.SuppressStatusMessages = true);
                services.AddLogging(logging =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));

#if DEBUG
                    logging.AddFilter<DebugLoggerProvider>(level => level >= Microsoft.Extensions.Logging.LogLevel.Debug);
                    logging.AddDebug();
#endif
                    logging.AddEventSourceLogger();
                    logging.AddSimpleConsole(config =>
                    {
                        config.ColorBehavior = LoggerColorBehavior.Enabled;
                        config.SingleLine = true;
                        config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
                        config.UseUtcTimestamp = false;
                    });

                    if (OperatingSystem.IsWindows())
                        logging.AddEventLog();

                    logging.Configure(options =>
                    {
                        options.ActivityTrackingOptions =
                            ActivityTrackingOptions.SpanId |
                            ActivityTrackingOptions.TraceId |
                            ActivityTrackingOptions.ParentId;
                    });
                });
            });

            return hostBuilder;
        }
    }
}
