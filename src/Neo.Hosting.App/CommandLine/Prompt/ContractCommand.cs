// Copyright (C) 2015-2024 The Neo Project.
//
// ContractCommand.cs file belongs to the neo project and is free
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
    internal sealed partial class ContractCommand : Command
    {
        public ContractCommand() : base("contract", "Invoke, destroy, update or deploy contact(s)")
        {
            var deployCommand = new DeployCommand();
            var updateCommand = new UpdateCommand();
            var invokeCommand = new InvokeCommand();
            var destroyCommand = new DestroyCommand();

            AddCommand(deployCommand);
            AddCommand(updateCommand);
            AddCommand(invokeCommand);
            AddCommand(destroyCommand);
        }
    }
}
