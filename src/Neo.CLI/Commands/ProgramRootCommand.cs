// Copyright (C) 2015-2024 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
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
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Neo.CLI.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo N3 Command-Line Tool")
        {

        }

        public new sealed class Handler : ICommandHandler
        {
            private readonly NamedPipeClientStream _namedPipeClientStream;

            public Handler()
            {
                _namedPipeClientStream = new NamedPipeClientStream(".", "LOCAL\\neo-cli", PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough);
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stoppingToken = context.GetCancellationToken();

                context.Console.Write("Connecting Named Pipe...");
                await _namedPipeClientStream.ConnectAsync();
                context.Console.WriteLine(" Connected!");
                Console.Clear();

                _ = TestAsync();
                await Console.OpenStandardInput().CopyToAsync(_namedPipeClientStream);

                return 0;
            }

            public async Task TestAsync()
            {
                await _namedPipeClientStream.CopyToAsync(Console.OpenStandardOutput());
            }
        }
    }
}
