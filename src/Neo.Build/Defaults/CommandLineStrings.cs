// Copyright (C) 2015-2025 The Neo Project.
//
// CommandLineStrings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Defaults
{
    internal static class CommandLineStrings
    {
        public static class Program
        {
            public const string RootDescription = "Neo Build Command-line Tool";
        }

        public static class Wallet
        {
            // Wallet Command
            public const string WalletDescription = "NEP6 Standard";

            // Wallet Create Command
            public const string CreateDescription = "Generates a NEP6 File.";
            public const string CreateFileDescription = "Save location of JSON file.";
            public const string CreateConfigFileDescription = "Read location of NeoBuild file.";
            public const string CreatePasswordDescription = "Password to encrypt private keys. [default: $(NeoWalletPassword)]";
            public const string CreateNameDescription = "Name for your wallet. [default: $(NeoWalletName)]";
            public const string CreateAddressVersionDescription = "Protocol address version to encode accounts. [default: $(NeoProtocolAddressVersion)]";
        }
    }
}
