// Copyright (C) 2015-2024 The Neo Project.
//
// ApplicationSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;
using Neo.Persistence;
using System;

namespace Neo.Service
{
    internal sealed class ApplicationSettings
    {
        public StorageSettings Storage { get; private init; } = StorageSettings.Default;
        public P2PSettings P2P { get; private init; } = P2PSettings.Default;
        public ContractsSettings Contracts { get; private init; } = ContractsSettings.Default;
        public NamedPipeSettings NamedPipe { get; private init; } = NamedPipeSettings.Default;
        public PluginSettings Plugin { get; private init; } = PluginSettings.Default;

        public static ApplicationSettings Default => new();

        public static ApplicationSettings Load(IConfigurationSection section) => new()
        {
            Storage = StorageSettings.Load(section.GetSection(nameof(Storage))),
            P2P = P2PSettings.Load(section.GetSection(nameof(P2P))),
            Contracts = ContractsSettings.Load(section.GetSection(nameof(Contracts))),
            NamedPipe = NamedPipeSettings.Load(section.GetSection(nameof(NamedPipe))),
            Plugin = PluginSettings.Load(section.GetSection(nameof(Plugin))),
        };
    }

    internal sealed class StorageSettings
    {
        public class ImportSettings
        {
            public string? Path { get; init; }
            public bool Verify { get; init; }

            internal static ImportSettings Default => new()
            {
                Verify = true,
            };

            internal static ImportSettings Load(IConfigurationSection section) => new()
            {
                Verify = section.GetValue(nameof(Verify), Default.Verify),
            };
        }

        public string? Engine { get; private init; }
        public string? Path { get; private init; }
        public ImportSettings Import { get; private init; } = ImportSettings.Default;

        public static StorageSettings Default => new()
        {
            Engine = nameof(MemoryStore),
            Import = ImportSettings.Default,
        };

        public static StorageSettings Load(IConfigurationSection section) => new()
        {
            Engine = section.GetValue(nameof(Engine), Default.Engine),
            Path = section.GetValue(nameof(Path), Default.Path),
            Import = ImportSettings.Load(section.GetSection(nameof(Import))),
        };
    }

    internal sealed class P2PSettings
    {
        public string? Listen { get; private init; }
        public ushort Port { get; private init; }
        public int MinDesiredConnections { get; private init; }
        public int MaxConnections { get; private init; }
        public int MaxConnectionsPerAddress { get; private init; }

        public static P2PSettings Default => new()
        {
            Listen = "0.0.0.0",
            Port = 10333,
            MinDesiredConnections = Peer.DefaultMinDesiredConnections,
            MaxConnections = Peer.DefaultMaxConnections,
            MaxConnectionsPerAddress = 3,
        };

        public static P2PSettings Load(IConfigurationSection section) => new()
        {
            Listen = section.GetValue(nameof(Listen), Default.Listen),
            Port = section.GetValue(nameof(Port), Default.Port),
            MinDesiredConnections = section.GetValue(nameof(MinDesiredConnections), Default.MinDesiredConnections),
            MaxConnections = section.GetValue(nameof(MaxConnections), Default.MaxConnections),
            MaxConnectionsPerAddress = section.GetValue(nameof(MaxConnectionsPerAddress), Default.MaxConnectionsPerAddress),
        };
    }

    internal sealed class ContractsSettings
    {
        public UInt160? NeoNameService { get; private init; }

        public static ContractsSettings Default => new();

        public static ContractsSettings Load(IConfigurationSection section) => new()
        {
            NeoNameService = NodeUtilities.TryParseUInt160(section.GetValue<string?>(nameof(NeoNameService))),
        };
    }

    internal sealed class NamedPipeSettings
    {
        public int Instances { get; private init; }
        public bool DebugMode { get; private init; }

        public static NamedPipeSettings Default => new()
        {
            Instances = 4,
            DebugMode = false,
        };

        public static NamedPipeSettings Load(IConfigurationSection section) => new()
        {
            Instances = section.GetValue(nameof(Instances), Default.Instances),
            DebugMode = section.GetValue(nameof(DebugMode), Default.DebugMode),
        };
    }

    internal sealed class PluginSettings
    {
        public string? DownloadUrl { get; private init; }
        public bool Prerelease { get; private init; }
        public Version? Version { get; private init; }

        public static PluginSettings Default => new()
        {
            DownloadUrl = "https://api.github.com/repos/neo-project/neo-modules/releases",
            Prerelease = false,
            Version = NodeUtilities.GetApplicationVersion(),
        };

        public static PluginSettings Load(IConfigurationSection section) => new()
        {
            DownloadUrl = section.GetValue(nameof(DownloadUrl), Default.DownloadUrl),
#if DEBUG
            Prerelease = section.GetValue(nameof(Prerelease), Default.Prerelease),
            Version = section.GetValue(nameof(Version), Default.Version),
#endif
        };
    }
}
