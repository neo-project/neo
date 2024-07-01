// Copyright (C) 2015-2024 The Neo Project.
//
// ContractCommand.Update.cs file belongs to the neo project and is free
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
using System.IO;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal partial class ContractCommand
    {
        internal sealed class UpdateCommand : Command
        {
            public UpdateCommand() : base("update", "Update contract")
            {
                var scriptHashArgument = new Argument<string>("SCRIPTHASH", "160-bit hash (hex)");
                var filePathArgument = new Argument<FileInfo>("FILE_PATH", "File path of the \".nef\"");
                var sendersScriptHashOption = new Option<string>(["--sender", "-s"], "Sender of the transaction");
                var manifestPathOption = new Option<FileInfo>(["--manifest", "-m"], "Manifest path of the \".json\"");
                var signerScriptHashesOption = new Option<string[]>(["--signers", "-sn"], "Signers of the transaction");
                var dataOption = new Option<string>(["--data", "-d"], "Data parameter of the contract method");

                AddArgument(scriptHashArgument);
                AddArgument(filePathArgument);
                AddOption(sendersScriptHashOption);
                AddOption(manifestPathOption);
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
