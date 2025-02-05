// Copyright (C) 2015-2025 The Neo Project.
//
// CreateWalletCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Defaults;
using System.CommandLine;
using System.IO;

namespace Neo.Build.Commands.Wallet
{
    internal partial class CreateWalletCommand : Command
    {
        public CreateWalletCommand() : base("create", CommandLineStrings.Wallet.CreateDescription)
        {
            var fileArgument = new Argument<FileInfo>("file", CommandLineStrings.Wallet.CreateFileDescription);
            var configFileArgument = new Argument<FileInfo>("configuration", CommandLineStrings.Wallet.CreateConfigFileDescription);
            var passwordOption = new Option<string>(["--password", "-p"], CommandLineStrings.Wallet.CreatePasswordDescription);
            var nameOption = new Option<string>(["--name", "-n"], CommandLineStrings.Wallet.CreateNameDescription);
            var addressVersionOption = new Option<byte>(["--version", "-v"], CommandLineStrings.Wallet.CreateAddressVersionDescription);

            AddArgument(fileArgument);
            AddArgument(configFileArgument);
            AddOption(passwordOption);
            AddOption(nameOption);
            AddOption(addressVersionOption);
        }
    }
}
