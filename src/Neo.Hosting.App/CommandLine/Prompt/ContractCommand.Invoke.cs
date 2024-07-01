// Copyright (C) 2015-2024 The Neo Project.
//
// ContractCommand.Invoke.cs file belongs to the neo project and is free
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
        internal sealed class InvokeCommand : Command
        {
            public InvokeCommand() : base("invoke", "Invoke method")
            {
                var scriptHashArgument = new Argument<string>("SCRIPTHASH", "160-bit hash (hex)");
                var methodNameArgument = new Argument<string>("METHOD_NAME", "Method name");
                var parametersOption = new Option<string[]>(["--params", "-p"], "Method parameter");
                var sendersScriptHashOption = new Option<string>(["--sender", "-s"], "Sender of the transaction");
                var signerScriptHashesOption = new Option<string[]>(["--signers", "-sn"], "Signers of the transaction");

                AddArgument(scriptHashArgument);
                AddArgument(methodNameArgument);
                AddOption(parametersOption);
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
