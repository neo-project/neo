// Copyright (C) 2015-2024 The Neo Project.
//
// Nep17Command.cs file belongs to the neo project and is free
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
    internal sealed partial class Nep17Command : Command
    {
        public Nep17Command() : base("nep17", "NEP-17 Commands")
        {
            var transferCommand = new TransferCommand();
            var balanceCommand = new BalanceCommand();
            var nameCommand = new NameCommand();
            var decimalsCommand = new DecimalsCommand();
            var totalSupplyCommand = new TotalSupplyCommand();

            AddCommand(transferCommand);
            AddCommand(balanceCommand);
            AddCommand(nameCommand);
            AddCommand(decimalsCommand);
            AddCommand(totalSupplyCommand);
        }
    }
}
