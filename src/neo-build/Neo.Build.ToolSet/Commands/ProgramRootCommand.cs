// Copyright (C) 2015-2025 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
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
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo Build Engine Command-line Tool")
        {
            var settingsCommand = new SettingsCommand();
            var nodeCommand = new NodeCommand();
            var createCommand = new CreateCommand();

            AddCommand(settingsCommand);
            AddCommand(nodeCommand);
            AddCommand(createCommand);
        }
    }
}
