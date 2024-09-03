// Copyright (C) 2015-2024 The Neo Project.
//
// ShowCommand.Classes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;

namespace Neo.CLI.Commands.Prompt
{
    internal partial class ShowCommand
    {
        private class StateCommand : Command
        {
            public StateCommand() : base("state", "Show the current state of the node.") { }
        }

        private class BlockCommand : Command
        {
            public BlockCommand() : base("block", "Show block state.")
            {
                var indexOrHashArgument = new Argument<string>("indexOrHash", "The block index or hash.");

                AddArgument(indexOrHashArgument);
            }
        }
    }
}
