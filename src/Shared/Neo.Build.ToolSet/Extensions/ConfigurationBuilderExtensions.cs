// Copyright (C) 2015-2025 The Neo Project.
//
// ConfigurationBuilderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Build.ToolSet.Configuration;
using System.Collections.Generic;

namespace Neo.Build.ToolSet.Extensions
{
    internal static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddNeoBuildConfiguration(this IConfigurationBuilder configBuilder)
        {
            configBuilder.Add(new NeoBuildConfigurationSource());
            return configBuilder;
        }

        public static IConfigurationBuilder AddNeoBuildConfiguration(this IConfigurationBuilder configBuilder, IEnumerable<KeyValuePair<string, string?>>? initialData)
        {
            configBuilder.Add(new NeoBuildConfigurationSource() { InitialData = initialData, });
            return configBuilder;
        }
    }
}
