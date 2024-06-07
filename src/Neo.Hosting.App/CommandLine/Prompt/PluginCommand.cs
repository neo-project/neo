// Copyright (C) 2015-2024 The Neo Project.
//
// PluginCommand.cs file belongs to the neo project and is free
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
    internal sealed partial class PluginCommand : Command
    {
        public PluginCommand() : base("plugin", "Manage plugins")
        {
            var installCommand = new InstallCommand();
            var uninstallCommand = new UninstallCommand();
            var listCommand = new ListCommand();

            AddCommand(installCommand);
            AddCommand(uninstallCommand);
            AddCommand(listCommand);
        }
    }
}
