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

using Neo.CLI.Extensions;
using Neo.CLI.Hosting.Services;
using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Threading;

namespace Neo.CLI.Commands.Prompt
{
    internal partial class ShowCommand : Command
    {
        public ShowCommand(
            NeoSystemHostedService neoSystemService,
            CancellationToken cancellationToken,
            IConsole console) : base("show", "Show node information.")
        {
            var stateCommand = new StateCommand
            {
                Handler = CommandHandler.Create(async () =>
                {
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    var stateTask = neoSystemService.ShowStateAsync(cts.Token);

                    Console.CursorVisible = false;

                    console.ReadLine();
                    cts.Cancel();

                    //await stateTask; // crashes thread

                    Console.CursorVisible = true;
                }),
            };

            AddCommand(stateCommand);
        }
    }
}
