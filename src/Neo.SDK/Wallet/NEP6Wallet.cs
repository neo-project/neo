// Copyright (C) 2015-2024 The Neo Project.
//
// NEP6Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security;

namespace Neo.SDK.Wallet
{
    public class NEP6Wallet
    {
        public string? Name { get; internal set; }
        public Version Version { get; internal set; } = new Version("1.0");
        public SecureString? Password { get; internal set; }
        public ScryptParameters Scrypt { get; internal set; } = ScryptParameters.Default;
        public IConfigurationSection? Extra { get; internal set; }

        public static NEP6Wallet Load(IConfigurationRoot section)
        {
            var wallet = new NEP6Wallet();
            var version = section.GetValue(JsonWalletDefaults.Version, Version.Parse("1.0"));

            if (version != wallet.Version)
                throw new InvalidDataException(JsonWalletDefaults.Version);

            wallet.Name = section.GetValue(JsonWalletDefaults.Name, string.Empty);
            wallet.Scrypt = section.GetValue(JsonWalletDefaults.Scrypt, ScryptParameters.Default)!;
            wallet.Extra = section.GetSection(JsonWalletDefaults.Extra);

            return wallet;
        }
    }
}
