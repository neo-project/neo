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
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Hosting.Services;
using System;

namespace Neo.Hosting.App.Extensions
{
    internal static class HostBuilderExtensions
    {
        public static IHostBuilder UseNeoServiceConfiguration(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection>? configure = null)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                //services.Configure<InvocationLifetimeOptions>(config => config.SuppressStatusMessages = true);
                services.Configure<NeoOptions>(context.Configuration);
                services.AddSingleton(ProtocolSettings.Load(context.Configuration.GetRequiredSection("ProtocolConfiguration")));
                services.AddSingleton<NeoSystemHostedService>();
                services.AddSingleton<PromptSystemHostedService>();

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
                var manager = new ConfigurationManager();
                manager.AddJsonFile($"config.{environmentName}.json", optional: false);

                IConfigurationBuilder builder = manager;
                builder.Add(new NeoConfigurationSource(manager.GetRequiredSection(NeoOptions.ConfigurationSectionName)));

                config.AddConfiguration(manager);

                configure?.Invoke(context, config);
            });

            return hostBuilder;
        }
    }
}
