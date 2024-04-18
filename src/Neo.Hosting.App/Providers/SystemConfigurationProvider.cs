// Copyright (C) 2015-2024 The Neo Project.
//
// SystemConfigurationProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;
using System;
using System.Collections.Generic;

namespace Neo.Hosting.App.Providers
{
    internal sealed class SystemConfigurationProvider(
        IConfigurationSection systemConfigurationSection) : ConfigurationProvider
    {
        public override void Load()
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                // Storage
                ["SystemOptions:Storage:Path"] = systemConfigurationSection["Storage:Path"] ?? "Data_LevelDB_{0:X2}",
                ["SystemOptions:Storage:Engine"] = systemConfigurationSection["Storage:Engine"] ?? "LevelDBStore",
                ["SystemOptions:Storage:Archive:Path"] = systemConfigurationSection["Storage:Archive:Path"] ?? AppContext.BaseDirectory,
                ["SystemOptions:Storage:Archive:FileName"] = systemConfigurationSection["Storage:Archive:FileName"] ?? "chain.0.acc.zip",
                ["SystemOptions:Storage:Archive:Verify"] = systemConfigurationSection["Storage:Archive:Verify"] ?? bool.TrueString,

                // P2P
                ["SystemOptions:P2P:Listen"] = systemConfigurationSection["P2P:Listen"] ?? "0.0.0.0",
                ["SystemOptions:P2P:Port"] = systemConfigurationSection["P2P:Port"] ?? "10333",
                ["SystemOptions:P2P:MinDesiredConnections"] = systemConfigurationSection["P2P:MinDesiredConnections"] ?? $"{Peer.DefaultMinDesiredConnections}",
                ["SystemOptions:P2P:MaxConnections"] = systemConfigurationSection["P2P:MaxConnections"] ?? $"{Peer.DefaultMaxConnections}",
                ["SystemOptions:P2P:MaxConnectionsPerAddress"] = systemConfigurationSection["P2P:MaxConnectionsPerAddress"] ?? "3",

                // Contracts
                ["SystemOptions:Contracts:NeoNameService"] = systemConfigurationSection["Contracts:NeoNameService"] ?? "0x50ac1c37690cc2cfc594472833cf57505d5f46de",

                // Plugin
                ["SystemOptions:Plugin:DownloadUrl"] = systemConfigurationSection["Plugin:DownloadUrl"] ?? "https://api.github.com/repos/neo-project/neo-modules/releases",
                ["SystemOptions:Plugin:Prerelease"] = systemConfigurationSection["Plugin:Prerelease"] ?? bool.FalseString,
                ["SystemOptions:Plugin:Version"] = systemConfigurationSection["Plugin:Version"] ?? $"{Program.ApplicationVersion}",

                // Wallets
                ["SystemOptions:Wallets"] = systemConfigurationSection["Wallets"],
            };
        }
    }
}
