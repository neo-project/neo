// Copyright (C) 2015-2024 The Neo Project.
//
// WalletCommand.Address.cs file belongs to the neo project and is free
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
    internal partial class WalletCommand
    {
        internal sealed partial class AddressCommand : Command
        {
            public AddressCommand() : base("address", "Create, delete and list addresses")
            {
                var createCommand = new CreateCommand();
                var deleteCommand = new DeleteCommand();
                var listCommand = new ListCommand();
                var importCommand = new ImportCommand();
                var exportCommand = new ExportCommand();

                AddCommand(createCommand);
                AddCommand(deleteCommand);
                AddCommand(listCommand);
                AddCommand(importCommand);
                AddCommand(exportCommand);
            }
        }
    }
}
