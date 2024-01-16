// Copyright (C) 2015-2024 The Neo Project.
//
// NodeSettings.cs file belongs to the neo project and is free
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

namespace Neo.Node.Service
{
    internal sealed class NodeSettings
    {
        public LoggerSettings Logger { get; private init; } = LoggerSettings.Default;
        public StorageSettings Storage { get; private init; } = StorageSettings.Default;
        public P2PSettings P2P { get; private init; } = P2PSettings.Default;
        public ContractsSettings Contracts { get; private init; } = ContractsSettings.Default;

        public static NodeSettings Default => new();

        public static NodeSettings Load(IConfigurationSection section) => new()
        {
            Logger = LoggerSettings.Load(section.GetSection(nameof(Logger))),
            Storage = StorageSettings.Load(section.GetSection(nameof(Storage))),
            P2P = P2PSettings.Load(section.GetSection(nameof(P2P))),
            Contracts = ContractsSettings.Load(section.GetSection(nameof(Contracts))),
        };
    }

    internal sealed class LoggerSettings
    {
        public string? Path { get; private init; }
        public bool ConsoleOutput { get; private init; }
        public bool Active { get; private init; }

        public static LoggerSettings Default => new()
        {
            Path = "logs",
            ConsoleOutput = false,
            Active = false,
        };

        public static LoggerSettings Load(IConfigurationSection section) => new()
        {
            Path = section.GetValue(nameof(Path), Default.Path),
            ConsoleOutput = section.GetValue(nameof(ConsoleOutput), Default.ConsoleOutput),
            Active = section.GetValue(nameof(Active), Default.Active),
        };
    }

    internal sealed class StorageSettings
    {
        public string? Engine { get; private init; }
        public string? Path { get; private init; }

        public static StorageSettings Default => new()
        {
            Engine = nameof(MemoryStore),
        };

        public static StorageSettings Load(IConfigurationSection section) => new()
        {
            Engine = section.GetValue(nameof(Engine), Default.Engine),
            Path = section.GetValue(nameof(Path), Default.Path),
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
}
