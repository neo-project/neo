// Copyright (C) 2015-2025 The Neo Project.
//
// CreateCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;

namespace Neo.Build.ToolSet.Commands
{
    internal class CreateCommand : Command
    {
        public CreateCommand() : base("create", "Create wallets, nodes and etc")
        {
            var createWalletCommand = new CreateWalletSubCommand();

            AddCommand(createWalletCommand);
        }
    }
}
