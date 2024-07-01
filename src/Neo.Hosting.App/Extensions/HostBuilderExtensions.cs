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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Host;
using Neo.Plugins;
using System;
using System.CommandLine.Hosting;
using System.IO;

namespace Neo.Hosting.App.Extensions
{
    internal static class HostBuilderExtensions
    {
        public static IHostBuilder UseNeoSystem(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var protocolSettingsSection = context.Configuration.GetSection("ProtocolConfiguration");
                var protocolSettings = NeoProtocolSettingsDefaults.MainNet;

                if (protocolSettingsSection.Exists() == false)
                    services.AddSingleton(protocolSettings);
                else
                {
                    protocolSettings = ProtocolSettings.Load(protocolSettingsSection);
                    services.AddSingleton(protocolSettings);

                }

                Plugin.LoadPlugins();

                var storageOptions = context.Configuration.Get<StorageOptions>();

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

                services.AddSingleton(new NeoSystem(protocolSettings, storageOptions?.Engine ?? NeoDefaults.StoreProviderName, storagePath));
            });

            return hostBuilder;
        }

        public static IHostBuilder UseNeoServices(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection>? configure = null)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.Configure<InvocationLifetimeOptions>(config => config.SuppressStatusMessages = true);
                services.Configure<NeoOptions>(context.Configuration);

                //services.AddSingleton<NamedPipeEndPoint>();
                //services.AddSingleton<NamedPipeServerListener>();
                //services.AddSingleton<NeoSystemHostedService>();
                //services.AddSingleton<NamedPipeSystemHostedService>();
                //services.AddSingleton<NamedPipeClientService>();

                configure?.Invoke(context, services);
            });

            return hostBuilder;
        }

        public static IHostBuilder UseNeoHostConfiguration(this IHostBuilder hostBuilder, Action<IConfigurationBuilder>? configure = null)
        {
            hostBuilder.ConfigureHostConfiguration(config =>
            {
                config.AddNeoHostConfiguration();

                configure?.Invoke(config);
            });

            return hostBuilder;
        }

        public static IHostBuilder UseNeoAppConfiguration(this IHostBuilder hostBuilder, Action<HostBuilderContext, IConfigurationBuilder>? configure = null)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddNeoAppConfiguration();

                var environmentName = context.HostingEnvironment.EnvironmentName;
                var jsonConfigFileName = $"config.{environmentName}.json";

                try
                {
                    var manager = new ConfigurationManager();
                    manager.AddJsonFile(jsonConfigFileName, optional: false);

                    IConfigurationBuilder builder = manager;
                    var appConfigSection = manager.GetSection(NeoJsonSectionNameDefaults.Application);

                    builder.Add(new NeoConfigurationSource(appConfigSection.Exists() ? appConfigSection : null));

                    config.AddConfiguration(manager);
                }
                catch (FileNotFoundException)
                {
                    throw;
                }



                configure?.Invoke(context, config);
            });

            return hostBuilder;
        }
    }
}
