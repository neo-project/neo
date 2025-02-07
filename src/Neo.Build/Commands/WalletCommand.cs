// Copyright (C) 2015-2025 The Neo Project.
//
// WalletCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Commands.Wallet;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Build.Commands
{
    internal class WalletCommand : Command
    {
        public WalletCommand() : base("wallet", CommandLineStrings.Wallet.WalletDescription)
        {
            var createCommand = new CreateWalletCommand();

            AddCommand(createCommand);
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
