// Copyright (C) 2015-2025 The Neo Project.
//
// NodeCommand.cs file belongs to the neo project and is free
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
    internal class NodeCommand : Command
    {
        public NodeCommand() : base("node", "Configure, stop or run nodes")
        {
            var runNodeCommand = new RunNodeSubCommand();

            AddCommand(runNodeCommand);
        }
    }
}
