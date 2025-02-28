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

using Microsoft.Extensions.Hosting;
using Neo.Build.ToolSet.Extensions;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base(CommandLineStrings.Program.RootDescription)
        {
        }

        public new sealed class Handler(IHostEnvironment env) : ICommandHandler
        {
            private readonly IHostEnvironment _env = env;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                context.Console.WriteLine("      Environment: {0}", _env.EnvironmentName);
                context.Console.WriteLine("Working Directory: {0}", _env.ContentRootPath);
                context.Console.WriteLine();

                return Task.FromResult(0);
            }
        }

    }
}
