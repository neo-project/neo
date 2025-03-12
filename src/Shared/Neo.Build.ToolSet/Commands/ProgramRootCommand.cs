// Copyright (C) 2015-2025 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Neo.Build.ToolSet.Extensions;
using Neo.Build.ToolSet.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base(CommandLineStrings.Program.RootDescription)
        {
        }

        public new sealed class Handler(
            IHostEnvironment env,
            NeoSystemOptions neoSystemOptions) : ICommandHandler
        {
            private readonly IHostEnvironment _env = env;
            private readonly NeoSystemOptions _neoSystemOptions = neoSystemOptions;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                context.Console.WriteLine("      Environment: {0}", _env.EnvironmentName);
                context.Console.WriteLine("     Content Root: {0}", _env.ContentRootPath);
                context.Console.WriteLine("       Store Root: {0}", _neoSystemOptions.Storage.StoreRoot);
                context.Console.WriteLine("  Checkpoint Root: {0}", _neoSystemOptions.Storage.CheckPointRoot);
                context.Console.WriteLine("           Listen: {0}", _neoSystemOptions.Network.Listen);
                context.Console.WriteLine("             Port: {0}", _neoSystemOptions.Network.Port);
                context.Console.WriteLine("  Max Connections: {0}", _neoSystemOptions.Network.MaxConnections);
                context.Console.WriteLine("  Min Connections: {0}", _neoSystemOptions.Network.MinDesiredConnections);
                context.Console.WriteLine("    Max Addresses: {0}", _neoSystemOptions.Network.MaxConnectionsPerAddress);
                context.Console.WriteLine();

                return Task.FromResult(0);
            }
        }

    }
}
