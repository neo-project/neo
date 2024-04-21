// Copyright (C) 2015-2024 The Neo Project.
//
// SystemOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Neo.Hosting.App.Configuration
{
    internal sealed class SystemOptions
    {
        public required StorageOptions Storage { get; set; }
        public required P2POptions P2P { get; set; }
        public required ContractsOptions Contracts { get; set; }
        public required PluginOptions Plugin { get; set; }
        public required List<WalletOptions> Wallets { get; set; }
    }

    internal sealed class StorageOptions
    {
        public class ArchiveSettings
        {
            public required string Path { get; set; }
            public required string FileName { get; set; }
            public required bool Verify { get; set; }
        }

        public required string Engine { get; set; }
        public required string Path { get; set; }
        public required ArchiveSettings Archive { get; set; }
    }

    internal sealed class P2POptions
    {
        public required string Listen { get; set; }
        public required ushort Port { get; set; }
        public required int MinDesiredConnections { get; set; }
        public required int MaxConnections { get; set; }
        public required int MaxConnectionsPerAddress { get; set; }
    }

    internal sealed class ContractsOptions
        (string neoNameService)
    {
        private static readonly string s_defualtNameServiceString = "0x50ac1c37690cc2cfc594472833cf57505d5f46de";
        private static readonly UInt160 s_defaultNameServiceScriptHash = UInt160.Parse(s_defualtNameServiceString);

        private UInt160 _neoNameService = s_defaultNameServiceScriptHash;

        public required UInt160 NeoNameService
        {
            get => _neoNameService;
            set => _neoNameService = ParseUtilities.TryParseUInt160(neoNameService) ?? s_defaultNameServiceScriptHash;
        }
    }

    internal sealed class PluginOptions
    {
        public required string DownloadUrl { get; set; }
        public required bool Prerelease { get; set; }
        public required Version Version { get; set; }
    }

    internal sealed class WalletOptions
        (string name, string path, string password, bool isActive)
    {
        private SecureString _encryptedPassword = new();

        public required string Name { get; set; } = name;
        public required FileInfo Path { get; set; } = new(path);
        public required bool IsActive { get; set; } = isActive;

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
    }
}
