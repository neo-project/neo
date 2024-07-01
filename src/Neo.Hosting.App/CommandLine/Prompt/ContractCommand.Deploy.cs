// Copyright (C) 2015-2024 The Neo Project.
//
// ContractCommand.Deploy.cs file belongs to the neo project and is free
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
        internal sealed class DeployCommand : Command
        {
            public DeployCommand() : base("deploy", "Deploy contract")
            {
                var filePathArgument = new Argument<FileInfo>("FILE_PATH", "File path of the \".nef\"");
                var manifestPathOption = new Option<FileInfo>(["--manifest", "-m"], "Manifest path of the \".json\"");
                var dataOption = new Option<string>(["--data", "-d"], "Data parameter of the contract method");

                AddArgument(filePathArgument);
                AddOption(dataOption);
                AddOption(manifestPathOption);

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
