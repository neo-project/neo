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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Neo.Build.ToolSet.Configuration;
using Neo.Build.ToolSet.Configuration.Converters;
using Neo.Build.ToolSet.Options;
using Neo.Build.ToolSet.Providers;
using Neo.Build.ToolSet.Services;
using System;
using System.ComponentModel;
using System.Net;

namespace Neo.Build.ToolSet.Extensions
{
    internal static class HostBuilderExtensions
    {
        public static IHostBuilder UseNeoBuildGlobalConfiguration(this IHostBuilder hostBuilder)
        {
            // Host Configuration
            hostBuilder.ConfigureHostConfiguration(static config =>
            {
                var manger = new ConfigurationManager();

                config.AddConfiguration(manger);
                config.AddNeoBuildConfiguration();
            });

            // Application Configuration
            hostBuilder.ConfigureAppConfiguration(static (context, config) =>
            {
                var environmentName = context.HostingEnvironment.EnvironmentName;

                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("config.json", optional: false); // default app settings file

                // JSON files overwrite environment variables
                var contentRoot = context.Configuration[HostDefaults.ContentRootKey]!;
                config.SetBasePath(contentRoot);
                config.AddJsonFile($"config.{environmentName}.json", optional: true);   // App settings file
                config.AddJsonFile($"system.{environmentName}.json", optional: true);   // NeoSystem settings file
                config.AddJsonFile($"protocol.{environmentName}.json", optional: true); // Protocol options file (Overwrites wallet configurations)
                config.AddJsonFile($"vm.{environmentName}.json", optional: true);       // ApplicationEngine settings file
                config.AddJsonFile($"dbft.{environmentName}.json", optional: true);     // DBFT plugin settings file
            });

            // Logging Configuration
            hostBuilder.ConfigureLogging(static (context, logging) =>
            {
                var isWindows = OperatingSystem.IsWindows();

                if (isWindows)
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= Microsoft.Extensions.Logging.LogLevel.Warning);

                logging.AddConfiguration(context.Configuration.GetSection("Logging"));

                logging.AddDebug();
                logging.AddEventSourceLogger();
                logging.AddNeoBuildConsole();

                if (isWindows)
                    logging.AddEventLog();

                logging.Configure(static options =>
                {
                    options.ActivityTrackingOptions =
                        ActivityTrackingOptions.SpanId |
                        ActivityTrackingOptions.TraceId |
                        ActivityTrackingOptions.ParentId;
                });
            });

            hostBuilder.UseDefaultServiceProvider(static (context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsLocalNet();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            // Neo Hosting Options
            hostBuilder.AddNeoHostingOptions();

            hostBuilder.ConfigureServices(static (context, services) =>
            {
                services.AddHostedService<WebSocketService>();
                services.AddSingleton<TraceApplicationEngineProvider>();

                // Add default services here
            });

            // Register TypeConverters for IConfiguration.Get<T>()
            TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeof(IPAddressTypeConverter)));

            return hostBuilder;
        }

        public static IHostBuilder AddNeoHostingOptions(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(static (context, services) =>
            {
                var appEngineSection = context.Configuration.GetSection("VM");
                var appEngineOptions = appEngineSection.Get<ApplicationEngineOptions>()!;

                var protocolSection = context.Configuration.GetSection("Protocol");
                var protocolOptions = protocolSection.Get<ProtocolOptions>()!;

                var networkSection = context.Configuration.GetSection("Network");
                var neoSystemNetworkOptions = networkSection.Get<NeoSystemNetworkOptions>()!;

                var storageSection = context.Configuration.GetSection("Storage");
                var neoSystemStorageOptions = storageSection.Get<NeoSystemStorageOptions>()!;

                var dbftSection = context.Configuration.GetSection("DBFT");
                var dbftOptions = dbftSection.Get<DBFTOptions>()!;

                var neoHostingOptions = new NeoConfigurationOptions()
                {
                    ApplicationEngineOptions = appEngineOptions,
                    ProtocolOptions = protocolOptions,
                    NetworkOptions = neoSystemNetworkOptions,
                    StorageOptions = neoSystemStorageOptions,
                    DBFTOptions = dbftOptions,
                };

                services.AddSingleton<INeoConfigurationOptions>(neoHostingOptions);
            });

            return hostBuilder;
        }
    }
}
