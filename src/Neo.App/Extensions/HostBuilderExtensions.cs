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
using Neo.App.Configuration;
using Neo.App.Configuration.Converters;
using Neo.App.Options;
using Neo.App.Services;
using Neo.Cryptography.ECC;
using System;
using System.ComponentModel;
using System.Net;

namespace Neo.App.Extensions
{
    internal static class HostBuilderExtensions
    {
        public static IHostBuilder UseNeoConfiguration(this IHostBuilder hostBuilder)
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

                // JSON files overwrite environment variables
                var contentRoot = context.Configuration[HostDefaults.ContentRootKey]!;
                config.SetBasePath(contentRoot);
                config.AddJsonFile("config.json", optional: true);
                config.AddJsonFile($"config.{environmentName}.json", optional: true);
            });

            // Logging Configuration
            hostBuilder.ConfigureLogging(static (context, logging) =>
            {
                var isWindows = OperatingSystem.IsWindows();

                if (isWindows)
                    logging.AddFilter<EventLogLoggerProvider>(static level => level >= Microsoft.Extensions.Logging.LogLevel.Warning);

                logging.AddConfiguration(context.Configuration.GetSection("Logging"));

                logging.AddDebug();
                logging.AddEventSourceLogger();
                logging.AddNeoConsole();

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
                var isDevelopment = context.HostingEnvironment.IsMainNet();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            // Neo Hosting Options
            hostBuilder.AddNeoHostingOptions();

            hostBuilder.ConfigureServices(static (context, services) =>
            {
                // Add default services here
                services.AddSingleton<NeoSystemHostedService>();
                services.AddHostedService(provider => provider.GetRequiredService<NeoSystemHostedService>());
            });

            // Register Type Converters for IConfiguration.Get<T>()
            TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeof(IPAddressTypeConverter)));
            TypeDescriptor.AddAttributes(typeof(ECPoint), new TypeConverterAttribute(typeof(ECPointTypeConverter)));

            return hostBuilder;
        }

        public static IHostBuilder AddNeoHostingOptions(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(static (context, services) =>
            {
                var protocolSection = context.Configuration.GetSection(ProtocolConfigurationNames.SectionName);
                var protocolOptions = protocolSection.Get<ProtocolOptions>()!;

                var applicationSection = context.Configuration.GetSection(ApplicationConfigurationNames.SectionName);
                var networkSection = applicationSection.GetSection(ApplicationConfigurationNames.P2PSectionName);
                var networkOptions = networkSection.Get<NetworkOptions>()!;

                var storageSection = applicationSection.GetSection(ApplicationConfigurationNames.StorageSectionName);
                var storageOptions = storageSection.Get<StorageOptions>()!;

                var neoConfigurationOptions = new NeoConfigurationOptions()
                {
                    StorageConfiguration = storageOptions,
                    NetworkConfiguration = networkOptions,
                    ProtocolConfiguration = protocolOptions,
                };

                services.AddSingleton(neoConfigurationOptions);
            });

            return hostBuilder;
        }
    }
}
