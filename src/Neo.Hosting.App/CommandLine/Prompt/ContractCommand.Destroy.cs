// Copyright (C) 2015-2024 The Neo Project.
//
// ContractCommand.Destroy.cs file belongs to the neo project and is free
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
    internal partial class ContractCommand
    {
        internal sealed class DestroyCommand : Command
        {
            public DestroyCommand() : base("destroy", "Destroy contract")
            {
                var scriptHashArgument = new Argument<string>("SCRIPTHASH", "160-bit hash (hex)");
                var sendersScriptHashOption = new Option<string>(["--sender", "-s"], "Sender of the transaction");
                var signerScriptHashesOption = new Option<string[]>(["--signers", "-sn"], "Signers of the transaction");

                AddArgument(scriptHashArgument);
                AddOption(sendersScriptHashOption);
                AddOption(signerScriptHashesOption);

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
