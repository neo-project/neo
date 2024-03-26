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
using Microsoft.Extensions.Hosting;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Hosting;
using System;

namespace Neo.Hosting.App
{
    public partial class Program
    {
        static IHostBuilder DefaultNeoHostBuilderFactory(string[] args) =>
            new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(
                    [
                        new(HostDefaults.EnvironmentKey, NeoEnvironments.MainNet),
                        new(HostDefaults.ContentRootKey, Environment.CurrentDirectory),
                    ]);

                    config.AddEnvironmentVariables("NEO_");
                    config.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    var hostingEnvironment = context.HostingEnvironment;
                    config.SetBasePath(AppContext.BaseDirectory);

                    config.AddJsonFile("config." + hostingEnvironment.EnvironmentName + ".json", optional: false);

                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);

                })
                .UseServiceProviderFactory((context) => new DefaultServiceProviderFactory(CreateDefaultNeoServiceProviderOptions(context)));

        static ServiceProviderOptions CreateDefaultNeoServiceProviderOptions(HostBuilderContext context)
        {
            var flag = context.HostingEnvironment.IsNeoDevNet();
            return new ServiceProviderOptions
            {
                ValidateScopes = flag,
                ValidateOnBuild = flag
            };
        }
    }
}
