// Copyright (C) 2015-2025 The Neo Project.
//
// WalletTaskConfiguration.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build;
using System.IO;
using System.Text.Json;

namespace Neo.Build.Configurations
{
    internal class WalletTaskConfiguration
    {
        private readonly FileInfo _configFile;
        private readonly JsonSerializerOptions _jsonDefaultOptions;

        public WalletTaskConfiguration(
            FileInfo configFile,
            JsonSerializerOptions? jsonSerializerOptions = default)
        {
            _configFile = configFile;
            _jsonDefaultOptions = jsonSerializerOptions ?? NeoBuildDefaults.JsonDefaultSerializerOptions;
        }
    }
}
