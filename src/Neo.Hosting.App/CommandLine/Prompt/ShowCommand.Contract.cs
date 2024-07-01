// Copyright (C) 2015-2024 The Neo Project.
//
// ShowCommand.Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal partial class ShowCommand
    {
        internal sealed class ContractCommand : Command
        {
            public ContractCommand() : base("contract", "Show contract")
            {
                var nameOrHashArgument = new Argument<string>("NAME_OR_HASH160", "Contract name or 160-bit hash (hex)");

                AddArgument(nameOrHashArgument);

                this.SetHandler(async context => await new Handler().InvokeAsync(context));
            }

            internal sealed new class Handler : ICommandHandler
            {
                public Task<int> InvokeAsync(InvocationContext context)
                {
                    return Task.FromResult(0);
                }

                public int Invoke(InvocationContext context)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
