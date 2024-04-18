// Copyright (C) 2015-2024 The Neo Project.
//
// ExportCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine
{
    internal sealed partial class ExportCommand : Command
    {
        public ExportCommand() : base("export", "Export data for offline use")
        {
            var blocksCommand = new BlocksExportCommand();
            AddCommand(blocksCommand);
        }
    }
}
