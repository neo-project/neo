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

using Neo.Hosting.App.Helpers;
using Neo.Network.P2P;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Neo.Hosting.App.Configuration
{
    internal sealed class NeoOptions
    {
        public static readonly string ConfigurationSectionName = "ApplicationConfiguration";

        public StorageOptions Storage { get; set; } = new();
        public P2POptions P2P { get; set; } = new();
        public ContractOptions Contract { get; set; } = new("0x50ac1c37690cc2cfc594472833cf57505d5f46de");
        public PluginOptions Plugin { get; set; } = new();
        public RemoteOptions Remote { get; set; } = new();
        public List<WalletOptions> Wallets { get; set; } = [];
    }

    internal sealed class StorageOptions
    {
        public static readonly string ConfigurationSectionName = "Storage";

        public class ArchiveSettings
        {
            public static readonly string ConfigurationSectionName = "Archive";

            public string Path { get; set; } = AppContext.BaseDirectory;
            public string FileName { get; set; } = "chain.0.acc.zip";
        }

        public string Engine { get; set; } = "LevelDBStore";
        public string Path { get; set; } = "Data_LevelDB_{0:X2}";
        public bool Verify { get; set; } = true;
        public ArchiveSettings Archive { get; set; } = new();
    }

    internal sealed class P2POptions
    {
        public static readonly string ConfigurationSectionName = "P2P";

        public string Listen { get; set; } = "0.0.0.0";
        public ushort Port { get; set; } = 10333;
        public int MinDesiredConnections { get; set; } = Peer.DefaultMinDesiredConnections;
        public int MaxConnections { get; set; } = Peer.DefaultMaxConnections;
        public int MaxConnectionsPerAddress { get; set; } = 3;
    }

    internal sealed class ContractOptions
        (string neoNameService)
    {
        public static readonly string ConfigurationSectionName = "Contract";

        private static readonly string s_defualtNameServiceString = "0x50ac1c37690cc2cfc594472833cf57505d5f46de";
        private static readonly UInt160 s_defaultNameServiceScriptHash = UInt160.Parse(s_defualtNameServiceString);

        private UInt160 _neoNameService = s_defaultNameServiceScriptHash;

        public UInt160 NeoNameService
        {
            get => _neoNameService;
            set => _neoNameService = ParseUtility.TryParseUInt160(neoNameService) ?? s_defaultNameServiceScriptHash;
        }
    }

    internal sealed class PluginOptions
    {
        public static readonly string ConfigurationSectionName = "Plugin";

        public string DownloadUrl { get; set; } = "https://api.github.com/repos/neo-project/neo-modules/releases";
        public bool Prerelease { get; set; } = false;
        public Version Version { get; set; } = new(0, 0);
    }

    internal sealed class RemoteOptions
    {
        public static readonly string ConfigurationSectionName = "Remote";

        public string PipeName { get; set; } = default!;
        public string MaxPipes { get; set; } = default!;
    }

    internal sealed class WalletOptions
        (string name, string path, string password, bool isActive)
    {
        public string Name { get; set; } = name;
        public FileInfo Path { get; set; } = new(path);
        public bool IsActive { get; set; } = isActive;

        public required SecureString Password
        {
            get => _encryptedPassword;
            set
            {
                var passwordOptionValue = password;

                unsafe
                {
                    fixed (char* passwordChars = passwordOptionValue)
                    {
                        var securePasswordString = new SecureString(passwordChars, passwordOptionValue.Length);
                        securePasswordString.MakeReadOnly();
                        _encryptedPassword = value = securePasswordString;
                    }
                }
            }
        }

        private SecureString _encryptedPassword = new();
    }
}
