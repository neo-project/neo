// Copyright (C) 2015-2024 The Neo Project.
//
// IConfigurationBuilderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Neo.Hosting.App.Helpers;
using Neo.Hosting.App.Host;
using System;

namespace Neo.Hosting.App.Extensions
{
    internal static class IConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddNeoHostConfiguration(this IConfigurationBuilder configBuilder)
        {
            _ = EnvironmentUtility.TryGetHostingEnvironment(out var hostEnvironment);

            configBuilder.AddInMemoryCollection(
                [
                    new(HostDefaults.EnvironmentKey, hostEnvironment ?? NeoHostingEnvironments.MainNet),
                    new(HostDefaults.ContentRootKey, Environment.CurrentDirectory),
                ]);

            configBuilder.SetBasePath(AppContext.BaseDirectory);

            return configBuilder;
        }

        public static IConfigurationBuilder AddNeoAppConfiguration(this IConfigurationBuilder configBuilder)
        {
            _ = EnvironmentUtility.TryGetHostingEnvironment(out var hostEnvironment);

            configBuilder.AddInMemoryCollection(
                [
                    new(HostDefaults.EnvironmentKey, hostEnvironment ?? NeoHostingEnvironments.MainNet),
                    new(HostDefaults.ContentRootKey, Environment.CurrentDirectory),
                ]);

            configBuilder.SetBasePath(AppContext.BaseDirectory);

            return configBuilder;
        }
    }
}
