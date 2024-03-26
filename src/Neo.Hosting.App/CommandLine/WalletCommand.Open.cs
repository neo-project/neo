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
using System.Security;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine
{
    internal partial class WalletCommand
    {
        internal sealed class OpenWalletCommand : Command
        {
            public OpenWalletCommand() : base("open", "Open a wallet to manage or use.")
            {
                var walletPathArgument = new Argument<FileInfo>("file", "Path to the json file");

                var walletPasswordOption = new Option<SecureString>(
                    new[] { "--password", "-p" },
                    parseArgument: result =>
                    {
                        var passwordOptionValue = result.Tokens[0].Value;

                        unsafe
                        {
                            fixed (char* passwordChars = passwordOptionValue)
                            {
                                var securePasswordString = new SecureString(passwordChars, passwordOptionValue.Length);
                                securePasswordString.IsReadOnly();
                                return securePasswordString;
                            }
                        }
                    },
                    description: "Wallet file password");

                AddArgument(walletPathArgument);
                AddOption(walletPasswordOption);
            }

            public new sealed class Handler : ICommandHandler
            {
                public required FileInfo File { get; set; }
                public SecureString? Password { get; set; }

                public Task<int> InvokeAsync(InvocationContext context)
                {
                    if (File.Exists == false)
                    {
                        context.Console.ErrorWriteLine($"File {File.Name} was not found.");
                        return Task.FromResult(1);
                    }

                    if (Password is null)
                    {
                        Password = context.Console.PromptPassword();
                    }

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
