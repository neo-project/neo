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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Neo.CLI.Hosting;
using Neo.CLI.Hosting.Services;
using Neo.CLI.Pipes;

namespace Neo.CLI.Extensions
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
        /// Injects the NeoSystem into the application.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseNeoSystem(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.ConfigureOptions<NeoOptionsSetup>();
                services.TryAddSingleton<NeoSystemHostedService>();
                //services.AddHostedService(provider => provider.GetRequiredService<NeoSystemHostedService>());
            });
            return hostBuilder;
        }

        /// <summary>
        /// Injects the <see cref="IHostedService"/> named pipe server for the application.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseNamedPipe(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.TryAddSingleton(new NamedPipeEndPoint(NeoDefaults.PipeName));
                services.TryAddSingleton<NamedPipeService>();
                //services.AddHostedService(provider => provider.GetRequiredService<NamedPipeService>());
            });
            return hostBuilder;
        }
    }
}
