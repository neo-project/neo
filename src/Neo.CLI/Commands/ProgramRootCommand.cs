// Copyright (C) 2015-2024 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
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
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.CLI.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo N3 Command-Line Tool")
        {

        }

        public new sealed class Handler
            (NeoSystemHostedService neoSystemService) : ICommandHandler
        {
            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public Task<int> InvokeAsync(InvocationContext context)
            {
                context.Console.WriteLine(neoSystemService.GetHelloWorld());
                return Task.FromResult(0);
            }
        }
    }
}
