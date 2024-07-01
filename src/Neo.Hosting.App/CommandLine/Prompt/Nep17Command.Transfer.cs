// Copyright (C) 2015-2024 The Neo Project.
//
// Nep17Command.Transfer.cs file belongs to the neo project and is free
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
    internal partial class Nep17Command
    {
        internal sealed class TransferCommand : Command
        {
            public TransferCommand() : base("transfer", "Transfer NEP-17 tokens")
            {
                var scriptHashArgument = new Argument<string>("SCRIPTHASH", "160-bit hash (hex)");
                var toSenderHashArgument = new Argument<string>("TO", "160-bit hash (hex)");
                var amountHashArgument = new Argument<decimal>("AMOUNT", "Total tokens");
                var fromSenderHashArgument = new Option<string>(["--from", "-f"], "160-bit hash (hex)");
                var signerScriptHashesOption = new Option<string[]>(["--signers", "-sn"], "Signers of the transaction");
                var dataOption = new Option<string>(["--data", "-d"], "Data parameter of the contract method");

                AddArgument(scriptHashArgument);
                AddArgument(toSenderHashArgument);
                AddArgument(amountHashArgument);
                AddOption(fromSenderHashArgument);
                AddOption(signerScriptHashesOption);
                AddOption(dataOption);

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
