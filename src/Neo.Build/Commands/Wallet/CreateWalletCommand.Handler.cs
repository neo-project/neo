// Copyright (C) 2015-2025 The Neo Project.
//
// CreateWalletCommand.Handler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Build.Commands.Wallet
{
    internal partial class CreateWalletCommand
    {
        public new sealed class Handler : ICommandHandler
        {
            [MaybeNull]
            public FileInfo File { get; set; }

            [MaybeNull]
            public FileInfo Configuration { get; set; }

            [MaybeNull]
            public string Password { get; set; }

            [MaybeNull]
            public string Name { get; set; }

            [NotNull]
            public byte Version { get; set; }

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                var isTextFile = context.Console.IsOutputRedirected;

                return Task.FromResult(0);
            }
        }
    }
}
