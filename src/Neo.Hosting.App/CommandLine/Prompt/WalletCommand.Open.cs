// Copyright (C) 2015-2024 The Neo Project.
//
// WalletCommand.Open.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal partial class WalletCommand
    {
        internal sealed class WalletOpenCommand : Command
        {
            public WalletOpenCommand() : base("open", "Open a wallet to manage or use")
            {
                var walletPathArgument = new Argument<FileInfo>("JSON_FILE", "Path to the json file");

                AddArgument(walletPathArgument);
                this.SetHandler(context => new Handler(walletPathArgument).InvokeAsync(context));
            }

            public new sealed class Handler
                (Argument<FileInfo> jsonFileInfoArgument) : ICommandHandler
            {
                public Task<int> InvokeAsync(InvocationContext context)
                {
                    var jsonFileInfo = context.ParseResult.GetValueForArgument(jsonFileInfoArgument);

                    if (jsonFileInfo.Exists == false)
                    {
                        context.Console.ErrorMessage($"File '{jsonFileInfo.FullName}' was not found.");
                        return Task.FromResult(1);
                    }

                    var password = context.Console.PromptPassword();

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
