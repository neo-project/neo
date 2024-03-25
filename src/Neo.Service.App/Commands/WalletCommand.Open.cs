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

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Service.App.Commands
{
    internal partial class WalletCommand
    {
        internal sealed class OpenWalletCommand : Command
        {
            public OpenWalletCommand() : base("open", "Open a wallet to manage or use.")
            {
                var walletPathArgument = new Argument<FileInfo>("file", "Path to the json file");
                var walletPasswordOption = new Option<string>(new[] { "--password", "-p" }, "Wallet file password")
                {
                    IsRequired = true
                };
                AddArgument(walletPathArgument);
                AddOption(walletPasswordOption);
            }

            public new sealed class Handler : ICommandHandler
            {
                public required FileInfo File { get; set; }
                public required string Password { get; set; }

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
