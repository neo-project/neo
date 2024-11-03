// Copyright (C) 2015-2024 The Neo Project.
//
// QuitCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;

namespace Neo.Service.Commands.Prompt
{
    internal sealed class QuitCommand : Command
    {
        public QuitCommand() : base("quit", $"Exit application.")
        {
            AddAlias("exit");
            this.SetHandler(context => context.ExitCode = -1);
        }
    }
}
