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
using Neo.Build.ToolSet.Configuration.Converters;
using Neo.Build.ToolSet.Options;
using System;
using System.ComponentModel;
using System.Net;

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

                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("config.json", optional: false); // default app settings file

                var contentRoot = context.Configuration[HostDefaults.ContentRootKey]!;
                config.SetBasePath(contentRoot);
                config.AddJsonFile($"config.{environmentName}.json", optional: true);   // App settings file
                config.AddJsonFile($"system.{environmentName}.json", optional: true);   // NeoSystem settings file
                config.AddJsonFile($"protocol.{environmentName}.json", optional: true); // ProtocolSettings file
                config.AddJsonFile($"vm.{environmentName}.json", optional: true);       // ApplicationEngine settings file
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

            hostBuilder.ConfigureServices((context, services) =>
            {
                var appEngineSection = context.Configuration.GetSection("VM");
                var appEngineOptions = appEngineSection.Get<AppEngineOptions>()!;

                services.AddSingleton(appEngineOptions);

                // Add default services here
            });

            // Register TypeConverters for IConfiguration.Get<T>()
            TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeof(IPAddressTypeConverter)));

            return hostBuilder;
        }

        public static IHostBuilder UseNeoSystem(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var protocolSection = context.Configuration.GetSection("Protocol");
                var neoSystemSection = context.Configuration.GetSection("NeoSystem");
                var neoSystemOptions = neoSystemSection.Get<NeoSystemOptions>()!;
                var protocolOptions = protocolSection.Get<NeoProtocolOptions>()!;

                services.AddSingleton(neoSystemOptions);
                services.AddSingleton(protocolOptions);

                // Implement NeoSystem here and inject service
            });

            return hostBuilder;
        }
    }
}
