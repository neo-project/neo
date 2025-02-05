// Copyright (C) 2015-2025 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Defaults;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Build.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base(CommandLineStrings.Program.RootDescription)
        {
            var walletCommand = new WalletCommand();

            AddCommand(walletCommand);
        }

        public new sealed class Handler : ICommandHandler
        {
            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
