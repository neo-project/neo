// Copyright (C) 2015-2024 The Neo Project.
//
// NeoOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Helpers;
using Neo.CLI.Hosting;
using Neo.Extensions;
using Neo.Network.P2P;
using System;
using System.Security;

namespace Neo.CLI.Configuration
{
    internal sealed class NeoOptions
    {
        public StorageOptions Storage { get; set; } = new();
        public P2POptions P2P { get; set; } = new();
        public ContractOptions Contract { get; set; } = new("0x50ac1c37690cc2cfc594472833cf57505d5f46de");
        public PluginOptions Plugin { get; set; } = new();
        public NeoLoggerOptions Logger { get; set; } = new();
        public WalletOptions UnlockWallet { get; set; } = new();
    }

    internal sealed class StorageOptions
    {
        public class ArchiveSettings
        {
            public string Path { get; set; } = AppContext.BaseDirectory;
            public string FileName { get; set; } = "chain.0.acc";
        }

        public string Engine { get; set; } = NeoDefaults.StoreProviderName;
        public string Path { get; set; } = "Data_LevelDB_{0:X2}";
        public bool Verify { get; set; } = true;
        public ArchiveSettings Archive { get; set; } = new();
    }

    internal sealed class P2POptions
    {
        public string Listen { get; set; } = "0.0.0.0";
        public ushort Port { get; set; } = 10333;
        public int MinDesiredConnections { get; set; } = Peer.DefaultMinDesiredConnections;
        public int MaxConnections { get; set; } = Peer.DefaultMaxConnections;
        public int MaxConnectionsPerAddress { get; set; } = 3;
    }

    internal sealed class ContractOptions
        (string neoNameService)
    {
        private UInt160 _neoNameService = UInt160.Parse("0x50ac1c37690cc2cfc594472833cf57505d5f46de");

        public UInt160 NeoNameService
        {
            get => _neoNameService;
            set => _neoNameService = ParseUtility.TryParseUInt160(neoNameService) ?? UInt160.Parse("0x50ac1c37690cc2cfc594472833cf57505d5f46de");
        }
    }

    internal sealed class PluginOptions
    {
        public string DownloadUrl { get; set; } = "https://api.github.com/repos/neo-project/neo/releases";
        public bool Prerelease { get; set; } = false;
        public Version Version { get; set; } = new(0, 0);
    }

    internal sealed class NeoLoggerOptions
    {
        public string Path { get; set; } = default!;
        public bool ConsoleOutput { get; set; } = false;
        public bool Active { get; set; } = false;
    }

    internal sealed class WalletOptions
    {
        public string Name { get; set; } = default!;
        public string Path { get; set; } = default!;
        public bool IsActive { get; set; } = false;

        public string? Password
        {
            get => _encryptedPassword.GetClearText();
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _encryptedPassword = value.ToSecureString();
            }
        }

        private SecureString _encryptedPassword = new();
    }
}
