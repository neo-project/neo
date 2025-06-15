// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildConfigurationSource.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Build.ToolSet.Providers;
using System.Collections.Generic;

namespace Neo.Build.ToolSet.Configuration
{
    internal class NeoBuildConfigurationSource : IConfigurationSource
    {
        public IEnumerable<KeyValuePair<string, string?>>? InitialData { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new NeoBuildConfigurationProvider(this);
    }
}
