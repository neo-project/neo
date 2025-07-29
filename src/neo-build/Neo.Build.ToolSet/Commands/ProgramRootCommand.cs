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
using Neo.Build.ToolSet.Configuration;
using Neo.Build.ToolSet.Extensions;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo Build Engine Command-line Tool")
        {
            var runNodeCommand = new RunNodeCommand();
            var createCommand = new CreateCommand();

            AddCommand(runNodeCommand);
            AddCommand(createCommand);
        }

        public new sealed class Handler(
            IHostEnvironment env,
            NeoConfigurationOptions configurationOptions) : ICommandHandler
        {
            private readonly IHostEnvironment _env = env;
            private readonly NeoConfigurationOptions _configurationOptions = configurationOptions;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).ConfigureAwait(false).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                context.Console.WriteLine("------- Global Settings -------");
                context.Console.WriteLine();
                context.Console.WriteLine("        Environment: {0}", _env.EnvironmentName);
                context.Console.WriteLine("       Content Root: {0}", _env.ContentRootPath);
                context.Console.WriteLine("         Store Root: {0}", _configurationOptions.StorageOptions.StoreRoot);
                context.Console.WriteLine("    Checkpoint Root: {0}", _configurationOptions.StorageOptions.CheckPointRoot);
                context.Console.WriteLine("             Listen: {0}", _configurationOptions.NetworkOptions.Listen);
                context.Console.WriteLine("               Port: {0}", _configurationOptions.NetworkOptions.Port);
                context.Console.WriteLine("    Max Connections: {0}", _configurationOptions.NetworkOptions.MaxConnections);
                context.Console.WriteLine("    Min Connections: {0}", _configurationOptions.NetworkOptions.MinDesiredConnections);
                context.Console.WriteLine("        Max Address: {0}", _configurationOptions.NetworkOptions.MaxConnectionsPerAddress);
                context.Console.WriteLine("            Max Gas: {0}", _configurationOptions.ApplicationEngineOptions.MaxGas);
                context.Console.WriteLine("            Network: {0}", _configurationOptions.ProtocolOptions.Network);
                context.Console.WriteLine("    Address Version: {0}", _configurationOptions.ProtocolOptions.AddressVersion);
                context.Console.WriteLine(" Block Milliseconds: {0}", _configurationOptions.ProtocolOptions.MillisecondsPerBlock);
                context.Console.WriteLine("   Max Transactions: {0}", _configurationOptions.ProtocolOptions.MaxTransactionsPerBlock);
                context.Console.WriteLine("        Max MemPool: {0}", _configurationOptions.ProtocolOptions.MemoryPoolMaxTransactions);
                context.Console.WriteLine("      Max Traceable: {0}", _configurationOptions.ProtocolOptions.MaxTraceableBlocks);
                context.Console.WriteLine("        Initial Gas: {0}", _configurationOptions.ProtocolOptions.InitialGasDistribution);
                context.Console.WriteLine();
                context.Console.WriteLine("--------- DBFT Plugin ---------");
                context.Console.WriteLine();
                context.Console.WriteLine("         Store Root: {0}", _configurationOptions.DBFTOptions.StoreRoot);
                context.Console.WriteLine("     Max Block Size: {0}", _configurationOptions.DBFTOptions.MaxBlockSize);
                context.Console.WriteLine("     Max System Fee: {0}", _configurationOptions.DBFTOptions.MaxBlockSystemFee);
                context.Console.WriteLine("       Recover Logs: {0}", _configurationOptions.DBFTOptions.IgnoreRecoveryLogs);
                context.Console.WriteLine("   Exception Policy: {0}", _configurationOptions.DBFTOptions.ExceptionPolicy);
                context.Console.WriteLine();

                return Task.FromResult(0);
            }
        }

    }
}
