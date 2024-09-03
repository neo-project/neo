// Copyright (C) 2015-2024 The Neo Project.
//
// ReplRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Hosting;
using Neo.CLI.Hosting.Services;
using System.CommandLine;
using System.Threading;

namespace Neo.CLI.Commands.Prompt
{
    internal sealed class ReplRootCommand : Command
    {
        private static string? s_executablePath;

        public ReplRootCommand(
            NeoSystemHostedService neoSystemService,
            CancellationToken cancellationToken,
            IConsole console) : base(ExecutableName, $"Neo N3 Command-Line Tool")
        {
            var helpCommand = new HelpCommand();
            var quitCommand = new QuitCommand();
            var showCommand = new ShowCommand(neoSystemService, cancellationToken, console);

            AddCommand(helpCommand);
            AddCommand(quitCommand);
            AddCommand(showCommand);
        }

        public static string ExecutableName => NeoDefaults.ConsolePromptName;

        public static string ExecutablePath =>
            s_executablePath ??= $"localhost";
    }
}
