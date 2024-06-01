// Copyright (C) 2015-2024 The Neo Project.
//
// ShowCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.Host.Service;
using System.CommandLine;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal sealed partial class ShowCommand : Command
    {
        public ShowCommand(
            ILoggerFactory loggerFactory,
            NamedPipeClientService clientService) : base("show", "Show information about service")
        {
            var versionCommand = new VersionCommand(loggerFactory, clientService);
            var blockCommand = new BlockCommand();

            AddCommand(blockCommand);
            AddCommand(versionCommand);
        }
    }
}
