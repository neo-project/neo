// Copyright (C) 2015-2024 The Neo Project.
//
// NeoConfigurationSource.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Hosting.App.Providers;

namespace Neo.Hosting.App.Configuration
{
    internal class NeoConfigurationSource
        (IConfigurationSection? configurationSection = null) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new NeoConfigurationProvider(configurationSection);
    }
}
