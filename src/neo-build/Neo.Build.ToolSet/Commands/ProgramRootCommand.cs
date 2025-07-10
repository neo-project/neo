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
            NeoSystemNetworkOptions neoSystemNetworkOptions,
            NeoSystemStorageOptions neoSystemStorageOptions,
            AppEngineOptions appEngineOptions,
            ProtocolOptions protocolOptions,
            DBFTOptions dBFTOptions) : ICommandHandler
        {
            private readonly IHostEnvironment _env = env;
            private readonly NeoSystemNetworkOptions _neoSystemNetworkOptions = neoSystemNetworkOptions;
            private readonly NeoSystemStorageOptions _neoSystemStorageOptions = neoSystemStorageOptions;
            private readonly AppEngineOptions _appEngineOptions = appEngineOptions;
            private readonly ProtocolOptions _protocolOptions = protocolOptions;
            private readonly DBFTOptions _dBFTOptions = dBFTOptions;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).ConfigureAwait(false).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                context.Console.WriteLine("------- Global Settings -------");
                context.Console.WriteLine();
                context.Console.WriteLine("        Environment: {0}", _env.EnvironmentName);
                context.Console.WriteLine("       Content Root: {0}", _env.ContentRootPath);
                context.Console.WriteLine("         Store Root: {0}", _neoSystemStorageOptions.StoreRoot);
                context.Console.WriteLine("    Checkpoint Root: {0}", _neoSystemStorageOptions.CheckPointRoot);
                context.Console.WriteLine("             Listen: {0}", _neoSystemNetworkOptions.Listen);
                context.Console.WriteLine("               Port: {0}", _neoSystemNetworkOptions.Port);
                context.Console.WriteLine("    Max Connections: {0}", _neoSystemNetworkOptions.MaxConnections);
                context.Console.WriteLine("    Min Connections: {0}", _neoSystemNetworkOptions.MinDesiredConnections);
                context.Console.WriteLine("        Max Address: {0}", _neoSystemNetworkOptions.MaxConnectionsPerAddress);
                context.Console.WriteLine("            Max Gas: {0}", _appEngineOptions.MaxGas);
                context.Console.WriteLine("            Network: {0}", _protocolOptions.Network);
                context.Console.WriteLine("    Address Version: {0}", _protocolOptions.AddressVersion);
                context.Console.WriteLine(" Block Milliseconds: {0}", _protocolOptions.MillisecondsPerBlock);
                context.Console.WriteLine("   Max Transactions: {0}", _protocolOptions.MaxTransactionsPerBlock);
                context.Console.WriteLine("        Max MemPool: {0}", _protocolOptions.MemoryPoolMaxTransactions);
                context.Console.WriteLine("      Max Traceable: {0}", _protocolOptions.MaxTraceableBlocks);
                context.Console.WriteLine("        Initial Gas: {0}", _protocolOptions.InitialGasDistribution);
                context.Console.WriteLine();
                context.Console.WriteLine("--------- DBFT Plugin ---------");
                context.Console.WriteLine();
                context.Console.WriteLine("         Store Root: {0}", _dBFTOptions.StoreRoot);
                context.Console.WriteLine("     Max Block Size: {0}", _dBFTOptions.MaxBlockSize);
                context.Console.WriteLine("     Max System Fee: {0}", _dBFTOptions.MaxBlockSystemFee);
                context.Console.WriteLine("       Recover Logs: {0}", _dBFTOptions.IgnoreRecoveryLogs);
                context.Console.WriteLine("   Exception Policy: {0}", _dBFTOptions.ExceptionPolicy);
                context.Console.WriteLine();

                return Task.FromResult(0);
            }
        }

    }
}
