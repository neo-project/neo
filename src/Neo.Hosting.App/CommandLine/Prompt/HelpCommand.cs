// Copyright (C) 2015-2024 The Neo Project.
//
// HelpCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal class HelpCommand : Command
    {
        public HelpCommand() : base("help", "Show help and usage information")
        {
            var walletCommand = new WalletCommand();
            AddCommand(walletCommand);
        }
    }
}
